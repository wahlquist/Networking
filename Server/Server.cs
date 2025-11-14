using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static TcpClient? player1;
    static TcpClient? player2;

    static int paddle1Y = 5;
    static int paddle2Y = 5;
    static int ballX = 20;
    static int ballY = 10;
    static int velX = 1;
    static int velY = 1;
    static int score1 = 0;
    static int score2 = 0;

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Pong server started.");

        // Accept Player 1
        player1 = server.AcceptTcpClient();
        Console.WriteLine("Player 1 connected.");
        Send(player1, "PLAYER:1");

        // Accept Player 2
        player2 = server.AcceptTcpClient();
        Console.WriteLine("Player 2 connected.");
        Send(player2, "PLAYER:2");

        // Start listener threads
        new Thread(() => HandlePlayer(player1, 1)).Start();
        new Thread(() => HandlePlayer(player2, 2)).Start();

        // Main game loop = 30 FPS
        while (true)
        {
            UpdateGame();
            BroadcastState();
            Thread.Sleep(33);
        }
    }

    static void HandlePlayer(TcpClient client, int id)
    {
        NetworkStream s = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                int bytes = s.Read(buffer, 0, buffer.Length);
                if (bytes == 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                if (msg == "MOVE:UP")
                {
                    if (id == 1) paddle1Y--;
                    else paddle2Y--;
                }
                else if (msg == "MOVE:DOWN")
                {
                    if (id == 1) paddle1Y++;
                    else paddle2Y++;
                }
            }
            catch
            {
                break;
            }
        }
    }

    static void UpdateGame()
    {
        ballX += velX;
        ballY += velY;

        // Bounce top/bottom
        if (ballY <= 0 || ballY >= 20)
            velY *= -1;

        // Left paddle collision
        if (ballX == 1 && ballY >= paddle1Y && ballY <= paddle1Y + 3)
            velX = 1;

        // Right paddle collision
        if (ballX == 38 && ballY >= paddle2Y && ballY <= paddle2Y + 3)
            velX = -1;

        // Score left
        if (ballX <= 0)
        {
            score2++;
            ResetBall();
        }

        // Score right
        if (ballX >= 40)
        {
            score1++;
            ResetBall();
        }
    }

    static void ResetBall()
    {
        ballX = 20;
        ballY = 10;
        velX *= -1;
    }

    static void BroadcastState()
    {
        string msg = $"STATE:{ballX}:{ballY}:{paddle1Y}:{paddle2Y}:{score1}:{score2}";
        if (player1 != null) Send(player1, msg);
        if (player2 != null) Send(player2, msg);
    }

    static void Send(TcpClient c, string msg)
    {
        byte[] b = Encoding.UTF8.GetBytes(msg);
        c.GetStream().Write(b);
    }
}
