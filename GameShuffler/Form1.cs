using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using GameShuffler;

namespace GameShuffler
{
    public partial class Form1 : Form
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        public enum ShowWindowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("User32.dll")]
        public static extern bool ShowWindow(IntPtr handle, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        int RemoveGameKeyId = 1;
        int RemoveGameKey = (int)Keys.PageDown;

        int NextGameKeyId = 2;
        int NextGameKey = (int)Keys.PageUp;

        Thread? shuffleThread = null;
        bool stopShuffle = false;

        int minShuffleTime = 0;
        int maxShuffleTime = 0;
        List<GameInfo> gamesToShuffle = new List<GameInfo>();

        GameInfo? currentGame = null;

        Random rand = new Random();

        private const string SettingsFilePath = "settings.json";
        private Settings settings = new Settings();

        public Form1()
        {
            InitializeComponent();
            LoadSettings();
            // GenerateGamesToShuffleList();
            // ResumeAllProcesses();
            RefreshList();
            RefreshDesiredGamesList();
        }

        private void RefreshList()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshList));
                return;
            }

            var allProcesses = Process.GetProcesses();
            var currentProcessId = Process.GetCurrentProcess().Id;
            runningProcessesSelectionList.Rows.Clear();
            foreach (var process in allProcesses.Where(process => !string.IsNullOrEmpty(process.MainWindowTitle) && process.Id != currentProcessId))
            {
                runningProcessesSelectionList.Rows.Add(false, process.MainWindowTitle, process.Id, process.ProcessName);
            }
        }

        private void RefreshDesiredGamesList()
        {
            desiredGamesList.Rows.Clear();
            foreach (var game in settings.DesiredGamesToShuffle)
            {
                var attachedGameName = game.ConnectedGame?.ProcessName ?? string.Empty;
                desiredGamesList.Rows.Add(game.WindowTitle, game.ProcessName, game.Pause, attachedGameName, game.MakeFullscreen);
            }
        }

        private void Refresh_Clicked(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void RunningProcessesSelectionList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                var row = runningProcessesSelectionList.Rows[e.RowIndex];
                var isSelected = Convert.ToBoolean(row.Cells[0].Value);
                var windowTitle = row.Cells[1].Value.ToString();
                var processName = row.Cells[3].Value.ToString();
                if (isSelected)
                {
                    foreach (var game in settings.DesiredGamesToShuffle)
                    {
                        if (game.ConnectedGame != null && game.ConnectedGame.ProcessName == processName)
                        {
                            game.ConnectedGame = null;
                        }
                    }

                    if (!settings.DesiredGamesToShuffle.Any(g => g.ProcessName == processName))
                    {
                        settings.DesiredGamesToShuffle.Add(new GameInfo(processName, windowTitle, true));
                    }
                }
                else if (!isSelected && settings.DesiredGamesToShuffle.Any(g => g.ProcessName == processName))
                {
                    settings.DesiredGamesToShuffle.RemoveAll(g => g.ProcessName == processName);
                }
                RefreshDesiredGamesList();
            }
        }

        private void RunningProcessesSelectionList_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (runningProcessesSelectionList.IsCurrentCellDirty)
            {
                runningProcessesSelectionList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DesiredGamesList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5 && e.RowIndex >= 0)
            {
                var row = desiredGamesList.Rows[e.RowIndex];
                var processName = row.Cells[1].Value.ToString();
                settings.DesiredGamesToShuffle.RemoveAll(g => g.ProcessName == processName);
                RefreshDesiredGamesList();
                RefreshList();
            }
        }

        private void DesiredGamesList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = desiredGamesList.Rows[e.RowIndex];
                var processName = row.Cells[1].Value.ToString();
                var desiredGame = settings.DesiredGamesToShuffle.FirstOrDefault(g => g.ProcessName == processName);

                if (desiredGame != null)
                {
                    if (e.ColumnIndex == 2)
                    {
                        var pause = Convert.ToBoolean(row.Cells[2].Value);
                        desiredGame.Pause = pause;
                    }
                    else if (e.ColumnIndex == 4)
                    {
                        var makeFullscreen = Convert.ToBoolean(row.Cells[4].Value);
                        desiredGame.MakeFullscreen = makeFullscreen;
                    }
                }
            }
        }

        private void DesiredGamesList_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (desiredGamesList.IsCurrentCellDirty)
            {
                desiredGamesList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void StartButton_Clicked(object sender, EventArgs e)
        {
            refreshButton.Enabled = false;
            runningProcessesSelectionList.Enabled = false;
            desiredGamesList.Enabled = false;
            startButton.Enabled = false;
            minTimeTextBox.Enabled = false;
            maxTimeTextBox.Enabled = false;
            stopButton.Enabled = true;

            if (!int.TryParse(minTimeTextBox.Text, out minShuffleTime) || minShuffleTime < 0)
            {
                MessageBox.Show("Minimum shuffle time must be a positive number");
                return;
            }

            if (!int.TryParse(maxTimeTextBox.Text, out maxShuffleTime) || maxShuffleTime < minShuffleTime)
            {
                MessageBox.Show("Maximum shuffle time must be a positive number greater than minimum shuffle time");
                return;
            }

            if (!RegisterHotKey(this.Handle, RemoveGameKeyId, 0x0000, RemoveGameKey))
            {
                MessageBox.Show("Failed to initialize remove game key");
                return;
            }

            if (!RegisterHotKey(this.Handle, NextGameKeyId, 0x0000, NextGameKey))
            {
                MessageBox.Show("Failed to initialize next game key");
                return;
            }

            settings.MinShuffleTime = minShuffleTime;
            settings.MaxShuffleTime = maxShuffleTime;
            SaveSettings();

            GenerateGamesToShuffleList();

            stopShuffle = false;
            shuffleThread = new Thread(ShuffleLoop);
            shuffleThread.Start();
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            StopShuffler();
        }

        private async void ShuffleLoop()
        {
            var processesToShuffle = new List<GameInfo>(gamesToShuffle); // gamesToShuffle is modified during the loop, so we need to make a copy
            foreach (var game in processesToShuffle)
            {
                SuspendGame(game);
            }

            while (gamesToShuffle.Any() && !stopShuffle)
            {
                GenerateGamesToShuffleList();

                if (gamesToShuffle.Count <= 1)
                {
                    StopShuffler();
                    return;
                }

                if (currentGame != null && gamesToShuffle.Count > 1 && currentGame.Process != null)
                {
                    ShowWindow(currentGame.Process.MainWindowHandle, ShowWindowCommands.SW_SHOWMINNOACTIVE);
                }

                StartNewGame();

                int shuffleTime = rand.Next(minShuffleTime, maxShuffleTime) * 1000;
                Debug.WriteLine($"Next shuffle in {shuffleTime / 1000} seconds.");
                for (int i = 0; i < shuffleTime; i += 100)
                {
                    if (stopShuffle)
                    {
                        return;
                    }
                    await Task.Delay(100);

                    // Check if the current game has exited
                    if (currentGame?.Process?.HasExited == true)
                    {
                        Debug.WriteLine($"Current game {currentGame.Process.ProcessName} has exited.");
                        gamesToShuffle.Remove(currentGame);
                        currentGame = null;
                        break;
                    }
                }
            }

            stopShuffle = false;
        }

        private void StartNewGame()
        {
            if (currentGame != null)
            {
                SuspendGame(currentGame);
            }

            GenerateGamesToShuffleList(); // Generate the shuffle list before starting a new game

            var newGame = currentGame;
            while (newGame == currentGame)
            {
                if (gamesToShuffle.Count <= 1)
                {
                    StopShuffler();
                    return;
                }

                var newGameIndex = rand.Next(gamesToShuffle.Count);
                newGame = gamesToShuffle[newGameIndex];
                if (gamesToShuffle.Count <= 1)
                {
                    break;
                }
                try
                {
                    if (newGame?.Process?.HasExited == true)
                    {
                        GenerateGamesToShuffleList();
                        newGame = gamesToShuffle.ElementAtOrDefault(newGameIndex);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
                {
                    GenerateGamesToShuffleList();
                    newGame = gamesToShuffle.ElementAtOrDefault(newGameIndex);
                }
            }

            if (newGame == null || newGame.Process == null)
            {
                return;
            }

            if (newGame != currentGame)
            {
                ResumeGame(newGame);
                if (newGame.MakeFullscreen)
                {
                    ShowWindow(newGame.Process.MainWindowHandle, ShowWindowCommands.SW_SHOWMAXIMIZED);
                }
                else
                {
                    ShowWindow(newGame.Process.MainWindowHandle, ShowWindowCommands.SW_RESTORE);
                }
                SetForegroundWindow(newGame.Process.MainWindowHandle);
                currentGame = newGame;
            }
        }

        private void StopShuffler()
        {
            stopShuffle = true;

            if (shuffleThread != null && shuffleThread.IsAlive)
            {
                shuffleThread.Join();
            }

            Invoke(new Action(() =>
            {
                refreshButton.Enabled = true;
                runningProcessesSelectionList.Enabled = true;
                desiredGamesList.Enabled = true;
                startButton.Enabled = true;
                minTimeTextBox.Enabled = true;
                maxTimeTextBox.Enabled = true;
                stopButton.Enabled = false;

                UnregisterHotKey(this.Handle, RemoveGameKeyId);
                UnregisterHotKey(this.Handle, NextGameKeyId);

                ResumeAllProcesses();
                gamesToShuffle.Clear();
                currentGame = null;
                RefreshList();
                RefreshDesiredGamesList();
            }));
        }

        private void GenerateGamesToShuffleList()
        {
            gamesToShuffle.Clear();
            Debug.WriteLine("Generating games to shuffle list...");
            Debug.WriteLine("Desired games to shuffle:");
            foreach (var game in settings.DesiredGamesToShuffle)
            {
                Debug.WriteLine($"- {game.ProcessName} (Pause: {game.Pause})");
            }

            var allProcesses = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle)).ToList();
            Debug.WriteLine("Running processes with main window title:");
            foreach (var process in allProcesses)
            {
                Debug.WriteLine($"- {process.ProcessName} (ID: {process.Id})");
            }

            foreach (var game in settings.DesiredGamesToShuffle)
            {
                Debug.WriteLine($"Checking desired game: {game.ProcessName}");
                var process = allProcesses.FirstOrDefault(p => p.ProcessName.Equals(game.ProcessName, StringComparison.OrdinalIgnoreCase) && p.MainWindowTitle.Equals(game.WindowTitle, StringComparison.OrdinalIgnoreCase));
                if (process != null)
                {
                    Debug.WriteLine($"Found exact match process: {process.ProcessName} (ID: {process.Id}) with window title: {process.MainWindowTitle}");
                }
                else
                {
                    process = allProcesses.FirstOrDefault(p => p.ProcessName.Equals(game.ProcessName, StringComparison.OrdinalIgnoreCase));
                    if (process != null)
                    {
                        Debug.WriteLine($"Found process by name: {process.ProcessName} (ID: {process.Id}) with window title: {process.MainWindowTitle}");
                    }
                }

                if (process != null)
                {
                    game.Process = process;
                    gamesToShuffle.Add(game);

                    if (game.ConnectedGame != null)
                    {
                        Debug.WriteLine($"Checking connected game: {game.ConnectedGame.ProcessName}");
                        var connectedProcess = allProcesses.FirstOrDefault(p => p.ProcessName.Equals(game.ConnectedGame.ProcessName, StringComparison.OrdinalIgnoreCase) &&
                                                                                p.MainWindowTitle.Contains(game.ConnectedGame.ProcessName, StringComparison.OrdinalIgnoreCase));
                        if (connectedProcess != null)
                        {
                            Debug.WriteLine($"Found connected game process: {connectedProcess.ProcessName} (ID: {connectedProcess.Id})");
                            game.ConnectedGame.Process = connectedProcess;
                        }
                        else
                        {
                            Debug.WriteLine($"Connected game process not found: {game.ConnectedGame.ProcessName}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Process not found: {game.ProcessName}");
                    game.Process = null;
                }
            }

            gamesToShuffle = gamesToShuffle.Where(g => g.Process != null && !IsSystemProcess(g.Process)).ToList();

            Debug.WriteLine("Games to shuffle:");
            foreach (var game in gamesToShuffle)
            {
                Debug.WriteLine($"- {game.Process?.ProcessName ?? "No Process"} (ID: {game.Process?.Id ?? -1})");
                if (game.ConnectedGame?.Process != null)
                {
                    Debug.WriteLine($"  Connected game: {game.ConnectedGame.Process.ProcessName} (ID: {game.ConnectedGame.Process.Id})");
                }
            }

            Debug.WriteLine($"Total games to shuffle: {gamesToShuffle.Count}");
            if (gamesToShuffle.Count <= 1)
            {
                StopShuffler();
                return;
            }

            RefreshList();
        }

        private bool IsSystemProcess(Process process)
        {
            var systemProcesses = new List<string> { "dwm", "explorer", "System", "Idle" };
            return systemProcesses.Contains(process.ProcessName);
        }

        private void SuspendGame(GameInfo gameInfo, HashSet<int>? pausedProcesses = null)
        {
            var process = gameInfo.Process;
            if (process == null)
            {
                GenerateGamesToShuffleList();
                process = gameInfo.Process;
            }

            if (process == null || !IsProcessInGamesToShuffle(process))
            {
                Debug.WriteLine($"[SuspendGame] Process {process?.ProcessName} ({process?.Id}) is not in the shuffle list or connected games and will not be paused.");
                return;
            }

            pausedProcesses ??= new HashSet<int>();

            if (pausedProcesses.Contains(process.Id))
            {
                Debug.WriteLine($"[SuspendGame] Process {process.ProcessName} ({process.Id}) is already paused.");
                return;
            }

            pausedProcesses.Add(process.Id);

            if (!gameInfo.Pause)
            {
                Debug.WriteLine($"[SuspendGame] Process {process.ProcessName} ({process.Id}) is set to not be paused.");
                return;
            }

            Debug.WriteLine($"[SuspendGame] Preparing to pause process {process.ProcessName} ({process.Id}).");
            if (gameInfo.ConnectedGame != null)
            {
                Debug.WriteLine($"[SuspendGame] Connected game: {gameInfo.ConnectedGame.ProcessName} (Process ID: {gameInfo.ConnectedGame.Process?.Id ?? -1}).");
            }
            else
            {
                Debug.WriteLine($"[SuspendGame] No connected game for process {process.ProcessName} ({process.Id}).");
            }

            if (process.HasExited)
            {
                GenerateGamesToShuffleList();
                process = GetGameInfoByProcess(process)?.Process;
                if (process == null || process.HasExited)
                {
                    return;
                }
            }

            Debug.WriteLine($"[SuspendGame] Pausing process {process.ProcessName} ({process.Id}).");

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    SuspendThread(pOpenThread);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
                {
                    Debug.WriteLine($"[SuspendGame] Failed to suspend thread {pT.Id} of process {process.ProcessName} ({process.Id}): {ex.Message}");
                }
                finally
                {
                    CloseHandle(pOpenThread);
                }
            }

            if (gameInfo.ConnectedGame?.Process != null)
            {
                Debug.WriteLine($"[SuspendGame] Pausing connected game {gameInfo.ConnectedGame.ProcessName} ({gameInfo.ConnectedGame.Process?.Id}).");
                SuspendGame(gameInfo.ConnectedGame, pausedProcesses);
            }
        }

        private void ResumeGame(GameInfo gameInfo, HashSet<int>? resumedProcesses = null)
        {
            var process = gameInfo.Process;
            if (process == null)
            {
                GenerateGamesToShuffleList();
                process = gameInfo.Process;
            }

            if (process == null) {
                Debug.WriteLine($"[ResumeGame] Process {gameInfo.ProcessName} could not be found.");
                return;
            }

            if (!IsProcessInGamesToShuffle(process))
            {
                Debug.WriteLine($"[ResumeGame] Process {gameInfo.ProcessName} is not in the shuffle list or connected games and will not be resumed.");
                return;
            }

            resumedProcesses ??= new HashSet<int>();

            if (resumedProcesses.Contains(process.Id))
            {
                Debug.WriteLine($"[ResumeGame] Process {process.ProcessName} ({process.Id}) is already resumed.");
                return;
            }

            resumedProcesses.Add(process.Id);

            Debug.WriteLine($"[ResumeGame] Preparing to resume process {process.ProcessName} ({process.Id}).");
            if (gameInfo.ConnectedGame != null)
            {
                Debug.WriteLine($"[ResumeGame] Connected game: {gameInfo.ConnectedGame.ProcessName} (Process ID: {gameInfo.ConnectedGame.Process?.Id ?? -1}).");
            }
            else
            {
                Debug.WriteLine($"[ResumeGame] No connected game for process {process.ProcessName} ({process.Id}).");
            }

            if (process.HasExited)
            {
                GenerateGamesToShuffleList();
                process = GetGameInfoByProcess(process)?.Process;
                if (process == null || process.HasExited)
                {
                    return;
                }
            }

            Debug.WriteLine($"[ResumeGame] Resuming process {process.ProcessName} ({process.Id}).");

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    var suspendCount = 0;
                    do
                    {
                        suspendCount = ResumeThread(pOpenThread);
                    } while (suspendCount > 0);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
                {
                    Debug.WriteLine($"[ResumeGame] Failed to resume thread {pT.Id} of process {process.ProcessName} ({process.Id}): {ex.Message}");
                }
                finally
                {
                    CloseHandle(pOpenThread);
                }
            }

            if (gameInfo.ConnectedGame?.Process != null)
            {
                Debug.WriteLine($"[ResumeGame] Resuming connected game {gameInfo.ConnectedGame.ProcessName} ({gameInfo.ConnectedGame.Process?.Id}).");
                ResumeGame(gameInfo.ConnectedGame, resumedProcesses);
            }
        }

        private bool IsProcessInGamesToShuffle(Process process)
        {
            return gamesToShuffle.Any(g => g.Process == process || g.ConnectedGame?.Process == process);
        }

        private GameInfo? GetGameInfoByProcess(Process process)
        {
            return gamesToShuffle.FirstOrDefault(g => g.Process == process || g.ConnectedGame?.Process == process);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = (int)m.WParam;

                if (id == RemoveGameKeyId && currentGame != null)
                {
                    try
                    {
                        if (currentGame.Process != null && !currentGame.Process.HasExited)
                        {
                            currentGame.Process.Kill();
                            currentGame.Process.Dispose();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"[WndProc] Failed to kill process {currentGame.Process?.ProcessName} ({currentGame.Process?.Id}): {ex.Message}");
                    }
                    gamesToShuffle.Remove(currentGame);
                    currentGame = null;
                    if (gamesToShuffle.Any())
                    {
                        StartNewGame();
                    }
                    RefreshList();
                }

                if (id == NextGameKeyId && gamesToShuffle.Any()) 
                {
                    StartNewGame();
                    RefreshList();
                }
            }

            base.WndProc(ref m);
        }

        private void SaveSettings()
        {
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFilePath, json);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();

                    minShuffleTime = settings.MinShuffleTime;
                    maxShuffleTime = settings.MaxShuffleTime;
                    minTimeTextBox.Text = minShuffleTime.ToString();
                    maxTimeTextBox.Text = maxShuffleTime.ToString();
                }
                catch (JsonException)
                {
                    ClearSettingsFile();
                    settings = new Settings();
                }
            }
        }

        private void ClearSettingsFile()
        {
            File.WriteAllText(SettingsFilePath, string.Empty);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            foreach (var game in gamesToShuffle)
            {
                ResumeGame(game);
            }

            SaveSettings();
        }

        private void ResumeAllProcesses()
        {
            foreach (var game in settings.DesiredGamesToShuffle)
            {
                if (game.Process != null)
                {
                    ResumeGame(game);
                }
                if (game.ConnectedGame?.Process != null)
                {
                    ResumeGame(game.ConnectedGame);
                }
            }
        }

        private void UpdateProcessInDataGridView(int index, Process newProcess)
        {
            foreach (DataGridViewRow row in runningProcessesSelectionList.Rows)
            {
                if ((int)row.Cells[3].Value == gamesToShuffle[index].Process?.Id)
                {
                    row.Cells[1].Value = newProcess.MainWindowTitle;
                    row.Cells[3].Value = newProcess.Id;
                    break;
                }
            }
        }

        private void RemoveProcessFromDataGridView(int index)
        {
            foreach (DataGridViewRow row in runningProcessesSelectionList.Rows)
            {
                if ((int)row.Cells[3].Value == gamesToShuffle[index].Process?.Id)
                {
                    runningProcessesSelectionList.Rows.Remove(row);
                    break;
                }
            }
        }

        private void RunningProcessesSelectionList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3 && e.RowIndex >= 0)
            {
                var row = runningProcessesSelectionList.Rows[e.RowIndex];
                var processName = row.Cells[3].Value.ToString();
                var windowTitle = row.Cells[1].Value.ToString();

                if (desiredGamesList.SelectedRows.Count > 0)
                {
                    var selectedRow = desiredGamesList.SelectedRows[0];
                    var selectedGameName = selectedRow.Cells[0].Value.ToString();
                    var selectedGame = settings.DesiredGamesToShuffle.FirstOrDefault(g => g.ProcessName == selectedGameName);

                    if (selectedGame != null)
                    {
                        if (selectedGame.ProcessName == processName)
                        {
                            MessageBox.Show("A game cannot be attached to itself.");
                            return;
                        }

                        if (settings.DesiredGamesToShuffle.Any(g => g.ProcessName == processName))
                        {
                            MessageBox.Show("This game is already in the desired games to shuffle list and cannot be attached.");
                            return;
                        }

                        selectedGame.ConnectedGame = new GameInfo(processName, windowTitle, true);
                        RefreshDesiredGamesList();
                    }
                    else
                    {
                        MessageBox.Show("Please select a valid game to attach this process to.");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a game in the desired games to shuffle list to attach this process to.");
                }
            }
        }
    }
}
