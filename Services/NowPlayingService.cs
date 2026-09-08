using ObsMusicPlayer.Models;
using ObsMusicPlayer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace ObsMusicPlayer.Services
{
    public class NowPlayingService : INowPlayingService
    {
        private readonly string _filePath;
        private readonly HttpListener _httpListener;
        private readonly Channel<NowPlayingInfo?> _broadcastChannel;
        private readonly List<WebSocket> _clients = new();
        private CancellationTokenSource _cts = new();
        private NowPlayingInfo? _currentInfo;

        public event Action<NowPlayingInfo?>? NowPlayingChanged;

        public NowPlayingService()
        {
            // Путь к текстовому файлу для OBS Text Source
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MyWpfMusicPlayer");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "now_playing.txt");

            // Канал для broadcast WebSocket клиентам
            _broadcastChannel = Channel.CreateBounded<NowPlayingInfo?>(10);

            // HTTP + WebSocket сервер на localhost:8765
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8765/");
            _httpListener.Start();

            _ = AcceptConnectionsAsync();
            _ = BroadcastToWebSocketsAsync();
        }

        public void UpdateNowPlaying(NowPlayingInfo info)
        {
            _currentInfo = info;

            // 1. Обновляем текстовый файл
            File.WriteAllText(_filePath, info.DisplayText);

            // 2. Уведомляем overlay окно
            NowPlayingChanged?.Invoke(info);

            // 3. Отправляем WebSocket клиентам
            _broadcastChannel.Writer.TryWrite(info);
        }

        public void Clear()
        {
            _currentInfo = null;
            File.WriteAllText(_filePath, "Nothing playing");
            NowPlayingChanged?.Invoke(null);
            _broadcastChannel.Writer.TryWrite(null);
        }

        private async Task AcceptConnectionsAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var wsContext = await context.AcceptWebSocketAsync(null);
                        _clients.Add(wsContext.WebSocket);
                        _ = HandleWebSocket(wsContext.WebSocket);
                    }
                    else if (context.Request.Url?.AbsolutePath == "/overlay.html")
                    {
                        await ServeOverlayPage(context);
                    }
                    else if (context.Request.Url?.AbsolutePath == "/now_playing")
                    {
                        await ServeJsonApi(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                catch (Exception) when (_cts.Token.IsCancellationRequested) { }
                catch (Exception) { }
            }
        }

        private async Task HandleWebSocket(WebSocket ws)
        {
            try
            {
                var buffer = new byte[1024];
                while (ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    await ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                }
            }
            catch { }
            finally
            {
                _clients.Remove(ws);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }

        private async Task BroadcastToWebSocketsAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var info = await _broadcastChannel.Reader.ReadAsync(_cts.Token);

                    // Добавьте настройки сериализации
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };

                    var json = JsonSerializer.Serialize(info, options);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    var disconnected = new List<WebSocket>();
                    foreach (var client in _clients)
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            try
                            {
                                await client.SendAsync(
                                    new ArraySegment<byte>(bytes),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            }
                            catch
                            {
                                disconnected.Add(client);
                            }
                        }
                        else
                        {
                            disconnected.Add(client);
                        }
                    }

                    foreach (var client in disconnected)
                        _clients.Remove(client);
                }
                catch { }
            }
        }

        private async Task ServeOverlayPage(HttpListenerContext context)
        {
            var html = GetOverlayHtml();
            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private async Task ServeJsonApi(HttpListenerContext context)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_currentInfo, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private string GetOverlayHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Now Playing Overlay</title>
    <style>
        * { 
            margin: 0; 
            padding: 0; 
            box-sizing: border-box; 
        }
        
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: transparent;
            overflow: hidden;
            height: 100vh;
            display: flex;
            justify-content: center;
            align-items: flex-end;
            padding-bottom: 50px;
        }
        
        .overlay {
            background: linear-gradient(135deg, rgba(26,26,46,0.95) 0%, rgba(22,33,62,0.95) 100%);
            border-radius: 15px;
            padding: 25px 50px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.6);
            border: 2px solid #0f3460;
            min-width: 450px;
            text-align: center;
            opacity: 0;
            transform: translateY(20px) scale(0.95);
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
            pointer-events: none;
        }
        
        .overlay.visible {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
        
        .now-playing-label {
            color: #0f3460;
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 2px;
            margin-bottom: 10px;
            background: #e94560;
            display: inline-block;
            padding: 4px 12px;
            border-radius: 10px;
        }
        
        .title {
            color: #e94560;
            font-size: 32px;
            font-weight: 700;
            margin-bottom: 10px;
            text-shadow: 0 2px 15px rgba(233, 69, 96, 0.4);
            max-width: 600px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        
        .artist {
            color: #fff;
            font-size: 20px;
            margin-bottom: 15px;
            opacity: 0.9;
            font-weight: 500;
        }
        
        .album {
            color: #a0a0a0;
            font-size: 14px;
            margin-bottom: 15px;
            font-style: italic;
        }
        
        .progress-container {
            background: rgba(15, 52, 96, 0.5);
            border-radius: 10px;
            padding: 8px 20px;
            display: inline-block;
        }
        
        .progress-text {
            color: #0f3460;
            font-size: 13px;
            font-weight: 600;
            background: #e94560;
            padding: 5px 15px;
            border-radius: 15px;
            display: inline-block;
        }
    </style>
</head>
<body>
    <div id='overlay' class='overlay'>
        <div class='now-playing-label'>Сейчас играет</div>
        <div class='title' id='title'>Nothing playing</div>
        <div class='artist' id='artist'></div>
        <div class='album' id='album'></div>
    </div>

    <script>
        console.log('[NowPlaying] Script loaded');
        
        const overlay = document.getElementById('overlay');
        const titleEl = document.getElementById('title');
        const artistEl = document.getElementById('artist');
        const albumEl = document.getElementById('album');
        
        console.log('[NowPlaying] DOM elements:', {
            overlay: !!overlay,
            title: !!titleEl,
            artist: !!artistEl,
            album: !!albumEl
        });
        
        let hideTimeout;
        let ws;
        let reconnectAttempts = 0;
        
        function connect() {
            console.log('[NowPlaying] Connecting to WebSocket...');
            
            try {
                ws = new WebSocket('ws://localhost:8765');
                
                ws.onopen = () => {
                    console.log('[NowPlaying] ✅ Connected!');
                    reconnectAttempts = 0;
                };
                
                ws.onmessage = (event) => {
                    console.log('[NowPlaying] 📩 Raw message:', event.data);
                    
                    try {
                        const data = JSON.parse(event.data);
                        console.log('[NowPlaying] 📦 Parsed data:', data);
                        console.log('[NowPlaying]  Data type:', typeof data);
                        console.log('[NowPlaying] 📦 Has title:', 'title' in data);
                        
                        if (data && data.title) {
                            console.log('[NowPlaying]  Updating overlay with:', data);
                            updateOverlay(data);
                        } else {
                            console.log('[NowPlaying] ⚠️ No title in data, hiding overlay');
                            hideOverlay();
                        }
                    } catch (e) {
                        console.error('[NowPlaying]  Parse error:', e);
                        console.error('[NowPlaying] ❌ Error stack:', e.stack);
                    }
                };
                
                ws.onerror = (error) => {
                    console.error('[NowPlaying] ❌ WebSocket error:', error);
                };
                
                ws.onclose = () => {
                    console.log('[NowPlaying] 🔌 Connection closed');
                    hideOverlay();
                    
                    if (reconnectAttempts < 10) {
                        reconnectAttempts++;
                        const delay = Math.min(1000 * Math.pow(2, reconnectAttempts), 10000);
                        console.log(`[NowPlaying] 🔄 Reconnecting in ${delay}ms (attempt ${reconnectAttempts})`);
                        setTimeout(connect, delay);
                    }
                };
            } catch (e) {
                console.error('[NowPlaying] ❌ Connection error:', e);
                setTimeout(connect, 2000);
            }
        }
        
        function updateOverlay(data) {
            console.log('[NowPlaying]  updateOverlay called');
            console.log('[NowPlaying] 🎨 Data received:', data);
            
            // Проверяем существование элементов
            if (!titleEl || !artistEl || !albumEl) {
                console.error('[NowPlaying] ❌ DOM elements not found!');
                return;
            }
            
            // Обновляем текст
            titleEl.textContent = data.title || 'Unknown Title';
            artistEl.textContent = data.artist || 'Unknown Artist';
            albumEl.textContent = data.album || '';
            
            console.log('[NowPlaying] ✅ Text updated:');
            console.log('  - Title:', titleEl.textContent);
            console.log('  - Artist:', artistEl.textContent);
            console.log('  - Album:', albumEl.textContent);
            
            // Показываем оверлей
            console.log('[NowPlaying] 📺 Adding visible class');
            console.log('[NowPlaying] 📺 Current classes:', overlay.className);
            overlay.classList.add('visible');
            console.log('[NowPlaying] 📺 New classes:', overlay.className);
            
            // Проверяем opacity
            const computedStyle = window.getComputedStyle(overlay);
            console.log('[NowPlaying] 👁️ Computed opacity:', computedStyle.opacity);
            console.log('[NowPlaying] ️ Computed transform:', computedStyle.transform);
            
            // Сбрасываем таймер скрытия
            if (hideTimeout) {
                clearTimeout(hideTimeout);
            }
            
            hideTimeout = setTimeout(() => {
                console.log('[NowPlaying] ⏰ Hiding overlay after timeout');
                hideOverlay();
            }, 5000);
        }
        
        function hideOverlay() {
            console.log('[NowPlaying]  Hiding overlay');
            overlay.classList.remove('visible');
            if (hideTimeout) {
                clearTimeout(hideTimeout);
                hideTimeout = null;
            }
        }
        
        // Тестовая функция для проверки
        window.testOverlay = function() {
            console.log('[NowPlaying] 🧪 Test function called');
            const testData = {
                title: 'Test Song',
                artist: 'Test Artist',
                album: 'Test Album',
            };
            updateOverlay(testData);
        };
        
        console.log('[NowPlaying] 🚀 Initializing...');
        connect();
        
        // Показываем информацию о странице
        console.log('[NowPlaying] 📄 Page info:');
        console.log('  - URL:', window.location.href);
        console.log('  - Ready state:', document.readyState);
    </script>
</body>
</html>";
        }

        public void Dispose()
        {
            _cts.Cancel();
            _httpListener.Stop();
            _httpListener.Close();
        }
    }
}
