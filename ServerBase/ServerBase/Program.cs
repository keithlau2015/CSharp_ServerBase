using Database;
using Network;

public class Program
{
    private static MongoDBManager databaseManager;
    private static Server gameServer;
    public static void Main(string[] args)
    {
        databaseManager = new MongoDBManager("mongodb+srv://keith:<password>@clusterdev.jgsxl.mongodb.net/?retryWrites=true&w=majority");
        gameServer = new Server();
    }
}