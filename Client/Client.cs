using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static int ballX, ballY;
    static int p1Y, p2Y;
    static int score1, score2;
    static int playerId = 0;

    static void Main()
    {
        TcpClient client = new TcpClient("127.0.0.1", 5000);
        NetworkStream stream = client.GetStream();

        // Listen thread
        new Thread(() => Listen(stream)).Start();

        while (playerId == 0) { }

        ConsoleKey key;
        while (true)
        {
            key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.W || key == ConsoleKey.UpArrow)
                Send(stream, "MOVE:UP");

            if (key == ConsoleKey.S || key == ConsoleKey.DownArrow)
                Send(stream, "MOVE:DOWN");
        }
    }

    static void Listen(NetworkStream s)
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytes = s.Read(buffer, 0, buffer.Length);
            string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

            if (msg.StartsWith("PLAYER"))
            {
                playerId = int.Parse(msg.Split(':')[1]);
                continue;
            }

            if (msg.StartsWith("STATE"))
            {
                string[] p = msg.Split(':');
                ballX = int.Parse(p[1]);
                ballY = int.Parse(p[2]);
                p1Y = int.Parse(p[3]);
                p2Y = int.Parse(p[4]);
                score1 = int.Parse(p[5]);
                score2 = int.Parse(p[6]);

                Draw();
            }
        }
    }

    static void Draw()
    {
        Console.Clear();

        // Draw score
        Console.WriteLine($"Player 1: {score1}    Player 2: {score2}\n");

        char[,] screen = new char[22, 42];

        // Fill with spaces
        for (int y = 0; y < 22; y++)
            for (int x = 0; x < 42; x++)
                screen[y, x] = ' ';

        // Ball
        screen[ballY, ballX] = 'O';

        // Player 1 paddle
        for (int i = 0; i < 4; i++)
            screen[p1Y + i, 0] = '|';

        // Player 2 paddle
        for (int i = 0; i < 4; i++)
            screen[p2Y + i, 41] = '|';

        // Render
        for (int y = 0; y < 22; y++)
        {
            for (int x = 0; x < 42; x++)
                Console.Write(screen[y, x]);
            Console.WriteLine();
        }
    }

    static void Send(NetworkStream s, string msg)
    {
        byte[] b = Encoding.UTF8.GetBytes(msg);
        s.Write(b);
    }
}
