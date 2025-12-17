using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class GuessServer
{
    static TcpClient? p1, p2;
    static int secret1 = -1;
    static int secret2 = -1;
    static int currentTurn = 0;  
    static bool gameOver = false;

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Number Guessing Server started.");

        // Accept Player 1
        p1 = server.AcceptTcpClient();
        Send(p1, "PLAYER:1");
        Send(p1, "WAITING"); // Tell player 1 to wait for player 2
        Console.WriteLine("Player 1 connected.");

        // Accept Player 2
        p2 = server.AcceptTcpClient();
        Send(p2, "PLAYER:2");
        Send(p2, "WAITING"); // Tell player 2 to wait for the game to start
        Console.WriteLine("Player 2 connected.");

        // Start listener threads
        new Thread(() => HandlePlayer(p1, 1)).Start();
        new Thread(() => HandlePlayer(p2, 2)).Start();
    }

    static void HandlePlayer(TcpClient client, int id)
    {
        NetworkStream s = client.GetStream();
        byte[] buffer = new byte[1024];
        StringBuilder sb = new StringBuilder();

        try
        {
            while (true)
            {
                int bytes = s.Read(buffer, 0, buffer.Length);
                if (bytes == 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytes));

                while (sb.ToString().Contains('\n'))
                {
                    int i = sb.ToString().IndexOf('\n');
                    string msg = sb.ToString(0, i).Trim();
                    sb.Remove(0, i + 1);

                    HandleMessage(id, msg);
                }
            }
        }
        catch { }
    }

    static void HandleMessage(int id, string msg)
    {
        if (gameOver) return;

        // Handle secret number submission
        if (msg.StartsWith("SECRET"))
        {
            int value = int.Parse(msg.Split(':')[1]);
            if (value < 1 || value > 100) return;

            if (id == 1 && secret1 == -1) secret1 = value;
            if (id == 2 && secret2 == -1) secret2 = value;

            // Both secrets are set, start the game
            if (secret1 != -1 && secret2 != -1)
            {
                currentTurn = 1;
                Send(p1, "TURN:YOU");
                Send(p2, "TURN:OPPONENT");
            }
            else
            {
                // Remind this player to wait if the other hasn't chosen yet
                Send(GetClient(id), "WAITING");
            }

            return;
        }

        // Handle guesses
        if (msg.StartsWith("GUESS"))
        {
            if (id != currentTurn) return;

            int guess = int.Parse(msg.Split(':')[1]);
            int target = (id == 1) ? secret2 : secret1;

            if (guess < target)
                Send(GetClient(id), "RESULT:Higher");
            else if (guess > target)
                Send(GetClient(id), "RESULT:Lower");
            else
            {
                Send(GetClient(id), "RESULT:CORRECT");
                Send(GetClient(id), "WIN");
                Send(GetOther(id), "LOSE");
                gameOver = true;
                return;
            }

            // Switch turns
            currentTurn = (currentTurn == 1) ? 2 : 1;
            Send(GetClient(currentTurn), "TURN:YOU");
            Send(GetOther(currentTurn), "TURN:OPPONENT");
        }
    }

    static TcpClient GetClient(int id) => (id == 1) ? p1! : p2!;
    static TcpClient GetOther(int id) => (id == 1) ? p2! : p1!;

    static void Send(TcpClient? c, string msg)
    {
        try
        {
            if (c == null || !c.Connected) return;
            byte[] b = Encoding.UTF8.GetBytes(msg + "\n");
            c.GetStream().Write(b, 0, b.Length);
        }
        catch { }
    }
}
