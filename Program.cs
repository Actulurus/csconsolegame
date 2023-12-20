using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.Json;

class Program
{
    static List<Object> Objects = new List<Object>();
    static Player player = new Player();
    static Random random = new Random();

    static float FRAMERATE = 1 / 24;
    static int PLAYER_SPEED = 2;
    static float OBJECT_SPEED = 0.3f;
    static float OBJECT_INTERVAL = 2f;
    static int MAX_OBJECT_SIZE = 16;
    static int MAX_PLAYER_SIZE = 48;
    static int REQUIRED_OBJECT_SIZE_TO_INCREASE_PLAYER_SIZE = 15;

    static int WIDTH = Console.WindowWidth;
    static int HEIGHT = Console.WindowHeight - 3; // -3 because the readline at the bottom takes up some space

    static int currentDirection = 0;
    static int score = 0;
    static string debugMessage = "";

    static bool AABB_Point(int pointX, int pointY, int targetX, int targetY, int width, int height)
    {
        int right = targetX + width - 1;
        int left = targetX;
        int top = targetY;
        int bottom = targetY + height - 1;

        return pointX >= left && pointX <= right && pointY >= top && pointY <= bottom;
    }

    static bool AABB(int aX, int aY, int bX, int bY, int aWidth, int aHeight, int bWidth, int bHeight)
    {
        int aRight = aX + aWidth - 1;
        int aLeft = aX;
        int aTop = aY;
        int aBottom = aY + aHeight - 1;

        int bRight = bX + bWidth - 1;
        int bLeft = bX;
        int bTop = bY;
        int bBottom = bY + bHeight - 1;

        return aLeft <= bRight && aRight >= bLeft && aTop <= bBottom && aBottom >= bTop;
    }

    static void Render()
    {
        Console.Clear();

        string output = "";
        bool playerColorClosed = true;

        for (int y = 0; y < HEIGHT; y++)
        {
            if (y == 0)
            {
                output += "\u001b[35mScore: " + score + "\u001b[0m | How to play: Move left and right to catch objects. Bigger object = more score. Very big objects increase your character's size." + "\n";

                continue;
            }

            for (int x = 0; x < WIDTH; x++)
            {
                if (!playerColorClosed)
                {
                    output += "\u001b[0m";
                    playerColorClosed = true;
                }

                bool isObject = false;

                foreach (Object obj in Objects)
                {
                    if (AABB_Point(x, y, obj.x, Convert.ToInt32(obj.y), obj.width, obj.height))
                    {
                        isObject = true;

                        break;
                    }
                }

                if (!isObject)
                {
                    if (AABB_Point(x, y, player.x, player.y, player.width, player.height))
                    {
                        isObject = true;
                        playerColorClosed = false;
                        output += "\u001b[94m";
                    }
                }

                if (x == WIDTH - 1)
                {
                    output += "|";
                }
                else if (y == HEIGHT - 1)
                {
                    output += "-";
                }
                else
                {
                    output += isObject ? "O" : " ";
                }
            }

            output += "\n";
        }

        Console.WriteLine(output);
    }

    static void Input()
    {
        var input = Console.ReadKey();

        switch (input.Key)
        {
            case ConsoleKey.A:
                currentDirection = -1;
                break;
            case ConsoleKey.D:
                currentDirection = 1;
                break;
            default:
                break;
        }
    }

    static void Loop()
    {
        long lastSpawnedObject = 0;

        while (true)
        {
            WIDTH = Console.WindowWidth;
            HEIGHT = Console.WindowHeight - 3; // -3 because the readline at the bottom takes up some space

            long unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            if (unixTimestamp - lastSpawnedObject >= OBJECT_INTERVAL)
            {
                CreateObject();

                lastSpawnedObject = unixTimestamp;
            }

            if (Console.KeyAvailable)
            {
                Input();
            }

            player.y = (int)Math.Floor((HEIGHT - 3) * 0.9);
            player.x = (int)Math.Clamp(player.x + currentDirection * PLAYER_SPEED, 0, WIDTH - player.width);

            foreach (Object obj in Objects.ToList())
            {
                obj.y += OBJECT_SPEED;

                if (obj.y + obj.height >= HEIGHT)
                {
                    score = Math.Max(0, score - obj.width * obj.height);
                    Objects.Remove(obj);
                }
                else if (AABB(player.x, player.y, obj.x, Convert.ToInt32(obj.y), player.width, player.height, obj.width, obj.height))
                {
                    score += obj.width * obj.height;
                    player.width = Math.Min(MAX_PLAYER_SIZE, player.width + (obj.width >= REQUIRED_OBJECT_SIZE_TO_INCREASE_PLAYER_SIZE ? 2 : 0));
                    Objects.Remove(obj);
                }
            }

            Render();

            Thread.Sleep(50);
        }
    }

    void setupPlayer()
    {
        player.x = 10;
        player.y = 0;
    }

    static void Main()
    {
        Console.WriteLine(WIDTH);
        Console.WriteLine(HEIGHT);
        Loop();
    }

    static void CreateObject()
    {
        Object obj = new Object();
        int size = random.Next(1, MAX_OBJECT_SIZE);

        obj.x = random.Next(0, WIDTH - obj.width);
        obj.y = 0;

        obj.width = size;
        obj.height = Convert.ToInt32(Math.Floor((double)size / 1.4));

        Objects.Add(obj);
    }
}

class Player
{
    public int x = Console.WindowWidth / 2 - 3;
    public int y = (int)Math.Floor((Console.WindowHeight - 3) * 0.9);
    public int height = 1;
    public int width = 6;
}

class Object
{
    public int x = 0;
    public float y = 0f;
    public int height = 2;
    public int width = 2;
}
