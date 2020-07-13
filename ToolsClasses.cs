using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Tools
{

    // Provides a set of usefel methods regarding int arrays
    public static class ArrayHelper
    {

        // Finds the min value and index of it in an int array
        public static (int min, int index) Min(int[] arr)
        {
            int min = arr[0];
            int index = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < min)
                {
                    min = arr[i];
                    index = i;
                }
            }
            return (min, index);
        }

        // Finds the max value and index of it in an int array
        public static (int max, int index) Max(int[] arr)
        {
            int max = arr[0];
            int index = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > max)
                {
                    max = arr[i];
                    index = i;
                }
            }
            return (max, index);
        }

        // Pretty prints an int array
        public static void PrintArray(int[] arr)
        {
            Array.ForEach(arr, i => Console.Write($"{i} "));
            Console.WriteLine();
        }

    }

    // Provides methods to create a hash from any given string
    public static class Security
    {

        // Hashes a string given to it returning the hashed value
        public static string Hash(string s)
        {
            int SaltSize = 16;
            int HashSize = 20;
            int Iterations = 10000;

            byte[] salt = new byte[SaltSize];
            new RNGCryptoServiceProvider().GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(s, salt, Iterations);
            var hash = pbkdf2.GetBytes(HashSize);

            var hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            var base64Hash = Convert.ToBase64String(hashBytes);

            return base64Hash;
        }

        // Checks whether a string matches the hash that it should, returning true or false
        public static bool Verify(string s, string hashedS)
        {
            int SaltSize = 16;
            int HashSize = 20;
            int Iterations = 10000;

            var hashBytes = Convert.FromBase64String(hashedS);

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            var pbkdf2 = new Rfc2898DeriveBytes(s, salt, Iterations);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            for (var i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }

    }

    // Provides an easy logging system
    public class Logger
    {

        private static readonly object _locker = new object();
        public string Filepath { get; set; }

        // Entry to class must point to a location for the log text file
        public Logger(string filePath)
        {
            Filepath = filePath;
        }

        // Input any string into the log file
        public void Log(string message)
        {
            try
            {
                string msg = $"{DateTime.Now.ToLongTimeString()}: {message}{Environment.NewLine}";
                lock (_locker)
                {
                    File.AppendAllText(this.Filepath, msg);
                }
            }
            catch (Exception)
            {
                // This is not good
                Environment.Exit(-1);
            }
        }

        // Remove all text from the log file
        public void ClearLog()
        {
            try
            {
                lock (_locker)
                {
                    File.WriteAllText(this.Filepath, "");
                }
            }
            catch (Exception)
            {
                Environment.Exit(-1);
            }
        }

    }

    // Provides a simple timer that runs on a separate thread and can be awaited 
    public class Timer
    {

        private CancellationTokenSource _source;

        // Starts the timer for any number of seconds
        public async Task Start(int seconds) => await Task.Run(async () =>
        {
            using (_source = new CancellationTokenSource())
            {
                while (seconds > 0)
                {
                    await Task.Delay(1000, _source.Token);
                    seconds--;
                }
            }
        });

        // Stops the timer and releases resources associated with it
        public void Stop()
        {
            _source.Cancel();
            _source.Dispose();
        }

    }

    // Provides a console command system that allows the user to create their own commands with just 
    // a public method, using reflection
    public class Commander
    {

        private readonly object obj;
        private readonly Type type;

        public List<string> Commands { get; set; } = new List<string>();

        // Takes an object reference that contains the method, static or otherwise.
        // Commander commander = new Commander(this);
        public Commander(object obj)
        {
            this.obj = obj;
        }

        // Takes a typeof(ClassName) where the static method is kept.
        // Commander commander = new Commander(typeof(Driver));
        public Commander(Type type)
        {
            this.type = type;
        }

        // Adds a new command to the controller
        // Method MUST be public
        public bool AddCommand(string methodName)
        {
            if (methodName.Contains(' '))
            {
                return false;
            }
            string command = char.ToUpper(methodName.Split(' ')[0][0]) + methodName.Split(' ')[0].Substring(1).ToLower();
            Commands.Add(command);
            return true;
        }

        // Starts accepting commands from the user
        public void Start()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            while (true)
            {
                Console.Write("> ");

                string rawInput = Console.ReadLine();
                if (rawInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (rawInput.Length > 0)
                {
                    string command = char.ToUpper(rawInput.Split(' ')[0][0]) + rawInput.Split(' ')[0].Substring(1).ToLower();
                    string[] args = rawInput.Split(' ').Skip(1).Take(rawInput.Split(' ').Length - 1).ToArray();
                    Array.ForEach(args, s => Console.WriteLine(s));
                    if (Commands.Contains(command))
                    {
                        try
                        {
                            if (obj == null)
                            {
                                // For static methods
                                MethodInfo m = type.GetMethod(command);
                                if (m.GetParameters().Length == args.Length)
                                {
                                    object result = m.Invoke(null, args);
                                }
                            }
                            else
                            {
                                MethodInfo m = obj.GetType().GetMethod(command);
                                if (m.GetParameters().Length == args.Length)
                                {
                                    object result = m.Invoke(obj, args);
                                }
                            }
                        }
                        catch (TargetParameterCountException)
                        {
                            Console.WriteLine("Invalid number of params");
                        }
                    }
                }
            }
        }
    }

}
