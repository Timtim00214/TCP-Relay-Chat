using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace AdvancedChat
{
    // 消息类型枚举
    public enum MessageType
    {
        Text,
        File,
        FileRequest,
        FileResponse,
        System
    }

    // 消息结构
    public class ChatMessage
    {
        public MessageType Type { get; set; }
        public string Content { get; set; }
        public string Sender { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    // 文件传输状态
    public class FileTransfer
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public long BytesTransferred { get; set; }
        public bool IsReceiving { get; set; }
        public FileStream FileStream { get; set; }
    }

    // 主程序类
    class ChatProgram
    {
        private static TcpClient client;
        private static NetworkStream stream;
        private static bool isConnected = false;
        private static bool isRunning = true;
        private static string localIp = "type your ip here";
        private static int localPort = 8888;
        private static string tunnelAddress = "xxxxxxxxxxxxxx.net";
        private static int tunnelPort = 55555;

        // 网络角色标识
        private static bool isServerMode = false;
        private static bool isClientMode = false;

        // UI相关
        private static StringBuilder inputBuffer = new StringBuilder();
        private static List<string> chatHistory = new List<string>();
        private static int chatAreaHeight = 15;
        private static int inputAreaTop;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // 文件传输相关
        private static Dictionary<string, FileTransfer> activeTransfers = new Dictionary<string, FileTransfer>();
        private static string fileSavePath = "./received_files/";

        static async Task Main(string[] args)
        {
            Console.Title = "高级聊天程序";
            Console.Clear();
            ShowWelcomeScreen();

            while (isRunning)
            {
                ShowMainMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RunAsServer();
                        break;
                    case "2":
                        await RunAsClient();
                        break;
                    case "3":
                        ShowHelp();
                        break;
                    case "4":
                        isRunning = false;
                        Console.WriteLine("再见！");
                        return;
                    default:
                        Console.WriteLine("无效选择，请重新输入");
                        break;
                }
            }
        }

        static void ShowWelcomeScreen()
        {
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║           高级聊天程序               ║");
            Console.WriteLine("║                                      ║");
            Console.WriteLine("║    本地地址: xxxxxxxxxxxxxxx.xxxx    ║");
            Console.WriteLine("║    穿透地址: xxxxxxxxxxxxxxxxxxxxxxx ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.WriteLine();
        }

        static void ShowMainMenu()
        {
            Console.WriteLine("请选择模式:");
            Console.WriteLine("1. 作为服务器 (等待别人连接)");
            Console.WriteLine("2. 作为客户端 (连接别人)");
            Console.WriteLine("3. 帮助信息");
            Console.WriteLine("4. 退出程序");
            Console.Write("请输入选择 (1-4): ");
        }

        static void ShowHelp()
        {
            Console.Clear();
            Console.WriteLine("=== 帮助信息 ===");
            Console.WriteLine("快捷键说明:");
            Console.WriteLine("  Enter    - 发送消息");
            Console.WriteLine("  Esc      - 返回主菜单");
            Console.WriteLine("  /file    - 发送文件 (在聊天中输入)");
            Console.WriteLine("  /exit    - 退出聊天");
            Console.WriteLine();
            Console.WriteLine("网络配置:");
            Console.WriteLine($"  本地监听: {localIp}:{localPort}");
            Console.WriteLine($"  穿透地址: {tunnelAddress}:{tunnelPort}");
            Console.WriteLine();
            Console.WriteLine("按任意键返回...");
            Console.ReadKey();
            Console.Clear();
        }

        // 服务器模式
        static async Task RunAsServer()
        {
            Console.Clear();
            Console.WriteLine("=== 服务器模式 ===");
            Console.WriteLine("你将作为服务端，等待朋友连接...");
            Console.WriteLine($"本地监听地址: {localIp}:{localPort}");
            Console.WriteLine($"请让你的朋友连接穿透地址: {tunnelAddress}:{tunnelPort}");
            Console.WriteLine("按任意键继续等待连接...");
            Console.ReadKey();

            try
            {
                isServerMode = true;
                isClientMode = false;

                IPAddress ipAddress = IPAddress.Parse(localIp);
                TcpListener listener = new TcpListener(ipAddress, localPort);
                listener.Start();

                Console.WriteLine($"服务器已启动，监听在 {localIp}:{localPort}");
                Console.WriteLine("等待客户端连接...");

                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                isConnected = true;

                Console.WriteLine("✅ 客户端已连接！进入聊天模式...");
                await StartChatSession("朋友");

                client?.Close();
                listener.Stop();
                isServerMode = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 服务器错误: {ex.Message}");
                Console.WriteLine("按任意键返回主菜单...");
                Console.ReadKey();
            }
        }

        // 客户端模式
        static async Task RunAsClient()
        {
            Console.Clear();
            Console.WriteLine("=== 客户端模式 ===");
            Console.WriteLine("你将作为客户端，连接朋友的服务器...");
            Console.WriteLine($"连接地址: {tunnelAddress}:{tunnelPort}");
            Console.WriteLine("按任意键开始连接...");
            Console.ReadKey();

            try
            {
                isClientMode = true;
                isServerMode = false;

                client = new TcpClient();
                Console.WriteLine($"正在连接 {tunnelAddress}:{tunnelPort} ...");

                await client.ConnectAsync(tunnelAddress, tunnelPort);
                stream = client.GetStream();
                isConnected = true;

                Console.WriteLine("✅ 连接成功！进入聊天模式...");
                await StartChatSession("朋友");

                client?.Close();
                isClientMode = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 客户端错误: {ex.Message}");
                Console.WriteLine("按任意键返回主菜单...");
                Console.ReadKey();
            }
        }

        // 聊天会话主循环
        static async Task StartChatSession(string partnerName)
        {
            // 确保接收文件的目录存在
            if (!Directory.Exists(fileSavePath))
            {
                Directory.CreateDirectory(fileSavePath);
            }

            Console.Clear();
            InitializeChatUI();

            // 启动消息接收线程
            var receiveTask = Task.Run(async () => await ReceiveMessages(partnerName));

            // 启动输入处理
            await HandleUserInput();

            // 清理资源
            cancellationTokenSource.Cancel();
            isConnected = false;
        }

        static void InitializeChatUI()
        {
            Console.Clear();

            // 绘制聊天区域边框
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            for (int i = 0; i < chatAreaHeight; i++)
            {
                Console.WriteLine("║                                                              ║");
            }
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ [输入消息]                                                   ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

            inputAreaTop = chatAreaHeight + 3;
            UpdateInputPrompt();
        }

        static void UpdateChatDisplay()
        {
            int startLine = 1;
            lock (chatHistory)
            {
                for (int i = 0; i < chatAreaHeight && i < chatHistory.Count; i++)
                {
                    int historyIndex = Math.Max(0, chatHistory.Count - chatAreaHeight + i);
                    string line = chatHistory[historyIndex].PadRight(60);
                    if (line.Length > 60) line = line.Substring(0, 57) + "...";

                    Console.SetCursorPosition(1, startLine + i);
                    Console.Write(line);
                }
            }
        }

        static void AddChatMessage(string message)
        {
            lock (chatHistory)
            {
                chatHistory.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (chatHistory.Count > 100) // 限制历史记录数量
                {
                    chatHistory.RemoveAt(0);
                }
            }
            UpdateChatDisplay();
        }

        static void UpdateInputPrompt()
        {
            Console.SetCursorPosition(1, inputAreaTop);
            Console.Write(new string(' ', 60)); // 清空输入行
            Console.SetCursorPosition(1, inputAreaTop);
            Console.Write(inputBuffer.ToString());
        }

        // 消息接收处理
        static async Task ReceiveMessages(string partnerName)
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            ProcessReceivedMessage(receivedData, partnerName);
                        }
                    }
                    else
                    {
                        await Task.Delay(50); // 避免CPU占用过高
                    }
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    AddChatMessage($"系统: 连接断开 - {ex.Message}");
                    isConnected = false;
                }
            }
        }

        static void ProcessReceivedMessage(string data, string partnerName)
        {
            try
            {
                // 解析消息协议
                if (data.StartsWith("MSG:"))
                {
                    // 普通文本消息
                    string message = data.Substring(4);
                    AddChatMessage($"{partnerName}: {message}");
                }
                else if (data.StartsWith("FILE_REQ:"))
                {
                    // 文件传输请求
                    string[] parts = data.Substring(9).Split('|');
                    if (parts.Length >= 2)
                    {
                        string fileName = parts[0];
                        long fileSize = long.Parse(parts[1]);
                        HandleFileRequest(fileName, fileSize);
                    }
                }
                else if (data.StartsWith("FILE_DATA:"))
                {
                    // 文件数据
                    HandleFileData(data.Substring(10));
                }
                else if (data.StartsWith("FILE_END:"))
                {
                    // 文件传输结束
                    string fileName = data.Substring(9);
                    HandleFileEnd(fileName);
                }
                else
                {
                    // 未知格式，当作普通消息处理
                    AddChatMessage($"{partnerName}: {data}");
                }
            }
            catch (Exception ex)
            {
                AddChatMessage($"系统: 消息解析错误 - {ex.Message}");
            }
        }

        static void HandleFileRequest(string fileName, long fileSize)
        {
            AddChatMessage($"系统: 对方请求发送文件 {fileName} ({fileSize} 字节)");
            // 自动接受文件传输
            string savePath = Path.Combine(fileSavePath, fileName);
            var transfer = new FileTransfer
            {
                FileName = fileName,
                FileSize = fileSize,
                BytesTransferred = 0,
                IsReceiving = true
            };

            try
            {
                transfer.FileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                activeTransfers[fileName] = transfer;
                AddChatMessage($"系统: 开始接收文件 {fileName}");
            }
            catch (Exception ex)
            {
                AddChatMessage($"系统: 文件创建失败 - {ex.Message}");
            }
        }

        static void HandleFileData(string data)
        {
            try
            {
                // 简单的文件数据处理（实际应该有更好的协议）
                string[] parts = data.Split('|', 2);
                if (parts.Length >= 2)
                {
                    string fileName = parts[0];
                    string base64Data = parts[1];

                    if (activeTransfers.ContainsKey(fileName) && activeTransfers[fileName].IsReceiving)
                    {
                        var transfer = activeTransfers[fileName];
                        byte[] fileBytes = Convert.FromBase64String(base64Data);

                        transfer.FileStream.Write(fileBytes, 0, fileBytes.Length);
                        transfer.BytesTransferred += fileBytes.Length;

                        // 显示进度
                        int progress = (int)((transfer.BytesTransferred * 100) / transfer.FileSize);
                        if (transfer.BytesTransferred % (transfer.FileSize / 10 + 1) == 0) // 每10%显示一次
                        {
                            AddChatMessage($"系统: 文件 {fileName} 接收进度 {progress}%");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddChatMessage($"系统: 文件数据处理错误 - {ex.Message}");
            }
        }

        static void HandleFileEnd(string fileName)
        {
            if (activeTransfers.ContainsKey(fileName) && activeTransfers[fileName].IsReceiving)
            {
                var transfer = activeTransfers[fileName];
                transfer.FileStream?.Close();
                activeTransfers.Remove(fileName);
                AddChatMessage($"✅ 文件 {fileName} 接收完成，保存在 {fileSavePath}");
            }
        }

        // 用户输入处理
        static async Task HandleUserInput()
        {
            inputBuffer.Clear();
            UpdateInputPrompt();

            while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Enter)
                    {
                        string message = inputBuffer.ToString().Trim();
                        if (!string.IsNullOrEmpty(message))
                        {
                            if (message.ToLower() == "/exit")
                            {
                                AddChatMessage("系统: 正在退出聊天...");
                                break;
                            }
                            else if (message.ToLower() == "/file")
                            {
                                await SendFile();
                            }
                            else
                            {
                                await SendMessage(message);
                                AddChatMessage($"我: {message}");
                            }
                        }
                        inputBuffer.Clear();
                        UpdateInputPrompt();
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (inputBuffer.Length > 0)
                        {
                            inputBuffer.Length--;
                            UpdateInputPrompt();
                        }
                    }
                    else if (key.Key == ConsoleKey.Escape)
                    {
                        AddChatMessage("系统: 正在退出聊天...");
                        break;
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        inputBuffer.Append(key.KeyChar);
                        UpdateInputPrompt();
                    }
                }
                else
                {
                    await Task.Delay(10); // 降低CPU使用率
                }
            }
        }

        static async Task SendMessage(string message)
        {
            try
            {
                string formattedMessage = $"MSG:{message}";
                byte[] data = Encoding.UTF8.GetBytes(formattedMessage);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                AddChatMessage($"发送失败: {ex.Message}");
            }
        }

        static async Task SendFile()
        {
            try
            {
                Console.SetCursorPosition(1, inputAreaTop);
                Console.Write(new string(' ', 60));
                Console.SetCursorPosition(1, inputAreaTop);
                Console.Write("请输入要发送的文件路径: ");

                string filePath = Console.ReadLine();

                if (string.IsNullOrEmpty(filePath))
                {
                    UpdateInputPrompt();
                    return;
                }

                if (!File.Exists(filePath))
                {
                    AddChatMessage("系统: 文件不存在");
                    UpdateInputPrompt();
                    return;
                }

                string fileName = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;

                // 发送文件请求
                string fileRequest = $"FILE_REQ:{fileName}|{fileSize}";
                byte[] requestData = Encoding.UTF8.GetBytes(fileRequest);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                AddChatMessage($"系统: 开始发送文件 {fileName} ({fileSize} 字节)");

                // 发送文件数据
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024];
                    long bytesSent = 0;
                    int bytesRead;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string base64Data = Convert.ToBase64String(buffer, 0, bytesRead);
                        string fileData = $"FILE_DATA:{fileName}|{base64Data}";
                        byte[] data = Encoding.UTF8.GetBytes(fileData);

                        await stream.WriteAsync(data, 0, data.Length);
                        bytesSent += bytesRead;

                        // 显示进度
                        int progress = (int)((bytesSent * 100) / fileSize);
                        if (bytesSent % (fileSize / 10 + 1) == 0) // 每10%显示一次
                        {
                            AddChatMessage($"系统: 文件发送进度 {progress}%");
                        }
                    }
                }

                // 发送文件结束信号
                string fileEnd = $"FILE_END:{fileName}";
                byte[] endData = Encoding.UTF8.GetBytes(fileEnd);
                await stream.WriteAsync(endData, 0, endData.Length);

                AddChatMessage($"✅ 文件 {fileName} 发送完成");
                UpdateInputPrompt();
            }
            catch (Exception ex)
            {
                AddChatMessage($"文件发送失败: {ex.Message}");
                UpdateInputPrompt();
            }
        }
    }
}