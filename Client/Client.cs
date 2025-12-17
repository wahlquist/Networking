using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class GuessClient
{
    
    static int playerId = 0;
    static bool myTurn = false;
    static bool choosingSecret = true;

    static void Main()
    {
        TcpClient client = new TcpClient("127.0.0.1", 5000);
        NetworkStream stream = client.GetStream();

        new Thread(() => Listen(stream)).Start();

        while (true)
{
    if (choosingSecret)
    {
        Console.Write("Choose your secret number (1–100): ");
        string secret = Console.ReadLine()!;
        Send(stream, $"SECRET:{secret}");
        choosingSecret = false; // done choosing secret
        continue;
    }

    if (!myTurn)
    {
        Thread.Sleep(100);
        continue;
    }

    // now it’s guessing turn
    Console.Write("Enter your guess: ");
    string guess = Console.ReadLine()!;
    Send(stream, $"GUESS:{guess}");
    myTurn = false;
}

    }

    static void Listen(NetworkStream s)
    {
        byte[] buffer = new byte[1024];
        StringBuilder sb = new();

        while (true)
        {
            int bytes = s.Read(buffer, 0, buffer.Length);
            sb.Append(Encoding.UTF8.GetString(buffer, 0, bytes));

            while (sb.ToString().Contains('\n'))
            {
                int i = sb.ToString().IndexOf('\n');
                string msg = sb.ToString(0, i).Trim();
                sb.Remove(0, i + 1);

                HandleMessage(msg);
            }
        }
    }

    static void HandleMessage(string msg)
    {
        if (msg.StartsWith("PLAYER"))
        {
            playerId = int.Parse(msg.Split(':')[1]);
            Console.WriteLine($"You are Player {playerId}");
        }

        if (msg == "TURN:YOU")
        {
            myTurn = true;
            Console.WriteLine("Your turn");
        }

        if (msg == "TURN:OPPONENT")
        {
            myTurn = false;
            Console.WriteLine("Opponent's turn...");
        }

        if (msg.StartsWith("RESULT"))
        {
            Console.WriteLine(msg.Split(':')[1]);
        }

        if (msg == "WIN")
        {
            Console.WriteLine("YOU WIN ");
            Environment.Exit(0);
        }

        if (msg == "LOSE")
        {
            Console.WriteLine("YOU LOSE 💀");
            Environment.Exit(0);
        }

        if (msg == "WAITING")
        {
            Console.WriteLine("Waiting for the other player to choose their secret...");
        }
    }

    static void Send(NetworkStream s, string msg)
    {
        byte[] b = Encoding.UTF8.GetBytes(msg + "\n");
        s.Write(b, 0, b.Length);
    }
}
