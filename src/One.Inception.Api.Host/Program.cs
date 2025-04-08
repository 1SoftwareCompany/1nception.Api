using System;

namespace One.Inception.Api.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = InceptionApi.GetHost();

            host.StartAsync();
            Console.WriteLine("Hello World!");
        }
    }
}
