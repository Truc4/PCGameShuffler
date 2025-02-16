using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;

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
        public static extern bool ShowWindow(IntPtr handle, int nCmdShow);

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
        List<Process> gamesToShuffle = new List<Process>();

        Process? currentGame = null;

        Random rand = new Random();

        private const string SettingsFilePath = "settings.json";
        private Settings settings = new Settings(); // Define an instance of the Settings class

        public Form1()
        {
            InitializeComponent();
            LoadSettings();
            RefreshList();
        }

        private void RefreshList()
        {
            var allProcesses = Process.GetProcesses();
            runningProcessesSelectionList.Rows.Clear();
            foreach (var process in allProcesses.Where(process => !string.IsNullOrEmpty(process.MainWindowTitle)))
            {
                var isSelected = settings.SelectedProcessIds.Contains(process.Id);
                runningProcessesSelectionList.Rows.Add(isSelected, process.MainWindowTitle, true, process.Id);
            }
        }

        private void Refresh_Clicked(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void StartButton_Clicked(object sender, EventArgs e)
        {
            refreshButton.Enabled = false;
            runningProcessesSelectionList.Enabled = false;
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

            settings.SelectedProcessIds.Clear();
            gamesToShuffle.Clear();
            foreach (DataGridViewRow row in runningProcessesSelectionList.Rows)
            {
                if (Convert.ToBoolean(row.Cells[0].Value))
                {
                    var processId = (int)row.Cells[3].Value;
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        settings.SelectedProcessIds.Add(processId);
                        gamesToShuffle.Add(process);
                    }
                    catch (ArgumentException)
                    {
                        // Process does not exist, skip it
                    }
                }
            }

            settings.MinShuffleTime = minShuffleTime;
            settings.MaxShuffleTime = maxShuffleTime;
            SaveSettings();

            shuffleThread = new Thread(ShuffleLoop);
            shuffleThread.Start();
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            refreshButton.Enabled = true;
            runningProcessesSelectionList.Enabled = true;
            startButton.Enabled = true;
            minTimeTextBox.Enabled = true;
            maxTimeTextBox.Enabled = true;
            stopButton.Enabled = false;

            if (shuffleThread != null)
            {
                stopShuffle = true;
                shuffleThread.Join();
            }

            UnregisterHotKey(this.Handle, RemoveGameKeyId);
            UnregisterHotKey(this.Handle, NextGameKeyId);

            foreach (var process in gamesToShuffle)
            {
                if (process != currentGame)
                {
                    ResumeProcess(process);
                    ShowWindow(process.MainWindowHandle, 3);
                }
                process.Dispose();
            }
            gamesToShuffle.Clear();
            currentGame = null;
        }

        private void ShuffleLoop()
        {
            var processesToShuffle = new List<Process>(gamesToShuffle); // gamesToShuffle is modified during the loop, so we need to make a copy
            foreach (var process in processesToShuffle)
            {
                var row = runningProcessesSelectionList.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (int)r.Cells[3].Value == process.Id);
                if (row != null && Convert.ToBoolean(row.Cells[2].Value))
                {
                    SuspendProcess(process);
                }
            }

            while (gamesToShuffle.Any() && !stopShuffle)
            {
                if (currentGame != null && gamesToShuffle.Count > 1)
                {
                    ShowWindow(currentGame.MainWindowHandle, 7);
                }

                StartNewGame();

                // Calculate a new shuffle time for each iteration
                int shuffleTime = rand.Next(minShuffleTime, maxShuffleTime) * 1000;
                int elapsedTime = 0;
                while (elapsedTime < shuffleTime)
                {
                    if (stopShuffle)
                    {
                        return;
                    }
                    Thread.Sleep(100);
                    elapsedTime += 100;

                    // Check if the current game has exited
                    if (currentGame?.HasExited == true)
                    {
                        Debug.WriteLine($"Current game {currentGame.ProcessName} has exited.");
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
                var row = runningProcessesSelectionList.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (int)r.Cells[3].Value == currentGame.Id);
                if (row != null && Convert.ToBoolean(row.Cells[2].Value))
                {
                    SuspendProcess(currentGame);
                }
            }

            var newGame = currentGame;
            while (newGame == currentGame)
            {
                var newGameIndex = rand.Next(gamesToShuffle.Count);
                newGame = gamesToShuffle[newGameIndex];
                if (gamesToShuffle.Count <= 1)
                {
                    break;
                }
                try
                {
                    if (newGame.HasExited)
                    {
                        gamesToShuffle.RemoveAt(newGameIndex);
                        newGame = currentGame;
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
                {
                    gamesToShuffle.RemoveAt(newGameIndex);
                    newGame = currentGame;
                }
            }

            if (newGame == null)
            {
                return;
            }

            if (newGame != currentGame)
            {
                ResumeProcess(newGame);
                ShowWindow(newGame.MainWindowHandle, 3);
                SetForegroundWindow(newGame.MainWindowHandle);
                currentGame = newGame;
            }
        }

        private void SuspendProcess(Process process)
        {
            if (!gamesToShuffle.Contains(process))
            {
                Debug.WriteLine($"[SuspendProcess] Process {process.ProcessName} ({process.Id}) is not in the shuffle list and will not be paused.");
                return;
            }

            if (process.HasExited)
            {
                gamesToShuffle.Remove(process);
                return;
            }

            Debug.WriteLine($"[SuspendProcess] Pausing process {process.ProcessName} ({process.Id}).");

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
                    Debug.WriteLine($"[SuspendProcess] Failed to suspend thread {pT.Id} of process {process.ProcessName} ({process.Id}): {ex.Message}");
                }
                finally
                {
                    CloseHandle(pOpenThread);
                }
            }
        }

        private void ResumeProcess(Process process)
        {
            if (!gamesToShuffle.Contains(process))
            {
                Debug.WriteLine($"[ResumeProcess] Process {process.ProcessName} ({process.Id}) is not in the shuffle list and will not be resumed.");
                return;
            }

            if (process.HasExited)
            {
                gamesToShuffle.Remove(process);
                return;
            }

            Debug.WriteLine($"[ResumeProcess] Resuming process {process.ProcessName} ({process.Id}).");

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
                    Debug.WriteLine($"[ResumeProcess] Failed to resume thread {pT.Id} of process {process.ProcessName} ({process.Id}): {ex.Message}");
                }
                finally
                {
                    CloseHandle(pOpenThread);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = (int)m.WParam;

                if (id == RemoveGameKeyId && currentGame != null)
                {
                    gamesToShuffle.Remove(currentGame);
                    currentGame.Kill();
                    currentGame.Dispose();
                    if (gamesToShuffle.Any())
                    {
                        StartNewGame();
                    }
                }

                if (id == NextGameKeyId && gamesToShuffle.Any()) 
                {
                    StartNewGame();
                }
            }

            base.WndProc(ref m);
        }

        private void SaveSettings()
        {
            var settings = new Settings
            {
                SelectedProcessIds = gamesToShuffle.Select(p => p.Id).ToList(),
                MinShuffleTime = minShuffleTime,
                MaxShuffleTime = maxShuffleTime
            };

            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFilePath, json);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();

                minShuffleTime = settings.MinShuffleTime;
                maxShuffleTime = settings.MaxShuffleTime;
                minTimeTextBox.Text = minShuffleTime.ToString();
                maxTimeTextBox.Text = maxShuffleTime.ToString();
            }
        }
    }
}
