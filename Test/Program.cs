
class Program
{
    static void Main(string[] args)
    {
        Action<string> test = testF;
        test += testF2;

        test.Invoke("test");
        Console.WriteLine($"Invoke::Main : {Thread.CurrentThread.ManagedThreadId}");

    }

    private static void testF(string obj)
    {
        Console.WriteLine($"{obj}::TestF : {Thread.CurrentThread.ManagedThreadId}");
    } 

    private static void testF2(string obj)
    {
        Console.WriteLine($"{obj}::TestF2 : {Thread.CurrentThread.ManagedThreadId}");
    }
}