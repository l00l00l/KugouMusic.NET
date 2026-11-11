using KuGou.Net.Clients;
using KugouAvaloniaPlayer.Models;
using KugouAvaloniaPlayer.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KugouAvaloniaPlayer.Services;

public sealed class ScheduleService : IDisposable
{
    private readonly ILogger<ScheduleService> _logger;
    private readonly PlaylistClient _playlistClient;

    private readonly IPlaybackCommands _playbackCommands;

    private readonly PlayerViewModel _playerViewModel;
    private readonly HashSet<string> _executedTasks = [];
    
    private readonly UserClient _userClient;

 

 

    private PeriodicTimer? _timer;

    private CancellationTokenSource? _cts;


    public ScheduleService(
    ILogger<ScheduleService> logger,
    PlaylistClient playlistClient,
    UserClient userClient,
    IPlaybackCommands playbackCommands,
    PlayerViewModel playerViewModel)
    {
        _logger = logger;
        _playlistClient = playlistClient;
        _userClient = userClient;
        _playbackCommands = playbackCommands;
        _playerViewModel = playerViewModel;
    }

    public void Start()
    {
        WriteDebugLog("ScheduleService Start");

        EnsureConfigExists();

        _cts = new CancellationTokenSource();

        _timer = new PeriodicTimer(
            TimeSpan.FromSeconds(10));

        _ = RunAsync(_cts.Token);
    }


    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task RunAsync(
    CancellationToken cancellationToken)
    {
        WriteDebugLog("RunAsync Started");

        try
        {
            while (await _timer!.WaitForNextTickAsync(
                       cancellationToken))
            {
                await CheckScheduleAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task CheckScheduleAsync()
    {
        var config = LoadConfig();

        if (config == null)
            return;

        var now = DateTime.Now;

        var currentTime =
            now.ToString("HH:mm");

        var currentDay =
            GetWeekDay(now.DayOfWeek);

        foreach (var task in config.Tasks)
        {
            if (!task.Enabled)
                continue;

            if (!task.WeekDays.Contains(currentDay))
                continue;

            if (!string.Equals(
                    task.Time,
                    currentTime,
                    StringComparison.Ordinal))
                continue;

            var taskKey =
                $"{now:yyyyMMddHHmm}_{task.Name}";

            if (!_executedTasks.Add(taskKey))
                continue;

            WriteDebugLog(
                $"Task Triggered: {task.Name}");

            await ExecuteTaskAsync(task);
        }

        CleanupExecutedTasks();
    }

    private async Task<string?> FindPlaylistIdAsync(
    string playlistName)
    {
        try
        {
            var playlists =
                await _userClient.GetPlaylistsAsync();

            var playlist =
                playlists?.Playlists?
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ListCreateId) &&
                        string.Equals(
                            x.Name,
                            playlistName,
                            StringComparison.OrdinalIgnoreCase));

            if (playlist == null)
            {
                WriteDebugLog(
                    $"Playlist Not Found: {playlistName}");

                return null;
            }

            WriteDebugLog(
                $"Playlist Found: {playlist.Name}, ListCreateId={playlist.ListCreateId}");

            return playlist.ListCreateId;
        }
        catch (Exception ex)
        {
            WriteDebugLog(
                $"FindPlaylist Failed: {ex}");

            return null;
        }
    }
    private async Task ExecuteTaskAsync(
    ScheduleTask task)
    {
        switch (task.Action)
        {
            case ScheduleActionType.Stop:

                WriteDebugLog(
                    $"Stop Playback: {task.Name}");

                _playbackCommands.Stop();

                break;

            case ScheduleActionType.Play:

                await ExecutePlayTaskAsync(task);

                break;
        }
    }

    private async Task ExecutePlayTaskAsync(
    ScheduleTask task)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    task.PlaylistName))
            {
                WriteDebugLog(
                    "PlaylistName  Empty");

                return;
            }

            WriteDebugLog(
                $"Loading Playlist: {task.PlaylistName}");

            var playlistId =
                await FindPlaylistIdAsync(
                    task.PlaylistName);

            if (string.IsNullOrWhiteSpace(playlistId))
            {
                WriteDebugLog(
                    $"Playlist Not Found: {task.PlaylistName}");

                return;
            }

            var data =
                await _playlistClient.GetSongsAsync(
                    playlistId,
                    pageSize: 1000);

            if (data?.Status != 1)
            {
                WriteDebugLog(
                    $"Load Playlist Failed");

                return;
            }

            var playlistSongs =
                data.Songs ?? [];

            if (playlistSongs.Count == 0)
            {
                WriteDebugLog(
                    "Playlist Empty");

                return;
            }

            var songs =
                playlistSongs.Select(x => new SongItem
                {
                    Name = x.Name,
                    Singer = x.Singers.Count > 0
                        ? string.Join("、",
                            x.Singers.Select(
                                s => s.Name))
                        : "未知",

                    Hash = x.Hash,

                    AlbumId = x.AlbumId,

                    FileId = x.FileId,

                    Cover = x.Cover,

                    DurationSeconds =
                        x.DurationMs / 1000.0,

                    Singers = x.Singers
                })
                .ToList();

            if (task.Volume.HasValue)
            {
                _playerViewModel.MusicVolume =
                    Math.Clamp(
                        task.Volume.Value / 100f,
                        0f,
                        1f);

                WriteDebugLog(
                    $"Set Volume={task.Volume}");
            }

            await _playbackCommands.ReplaceQueueAsync(
                songs,
                songs[0]);

            WriteDebugLog(
                $"Play Success, Songs={songs.Count}");
        }
        catch (Exception ex)
        {
            WriteDebugLog(
                $"Play Failed: {ex}");
        }
    }

    private void CleanupExecutedTasks()
    {
        if (_executedTasks.Count < 500)
            return;

        _executedTasks.Clear();

        WriteDebugLog(
            "Executed task cache cleared.");
    }
    private void WriteDebugLog(string message)
    {
        try
        {
            var logFile = Path.Combine(
                AppContext.BaseDirectory,
                "schedule.log");

            File.AppendAllText(
                logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 忽略日志异常
        }
    }
    private ScheduleConfig? LoadConfig()
    {
        try
        {
            var file = Path.Combine(
                AppContext.BaseDirectory,
                "schedule.json");

            if (!File.Exists(file))
                return null;

            var json = File.ReadAllText(file);

            return JsonSerializer.Deserialize(
                json,
                ScheduleJsonContext.Default.ScheduleConfig);
        }
        catch (Exception ex)
        {
            WriteDebugLog(
                $"LoadConfig Failed: {ex}");

            return null;
        }
    }

    private static int GetWeekDay(
        DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            _ => 7
        };
    }



    private static void EnsureConfigExists()
    {
        var basePath =
            AppContext.BaseDirectory;

        var configFile =
            Path.Combine(
                basePath,
                "schedule.json");

        var templateFile =
            Path.Combine(
                basePath,
                "schedule.template.json");

        if (File.Exists(configFile))
            return;

        if (!File.Exists(templateFile))
            return;

        File.Copy(
            templateFile,
            configFile,
            overwrite: false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
    }
}