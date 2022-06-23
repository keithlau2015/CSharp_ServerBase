using Database;
using Network;

public class Service
{
    private static MongoDBManager databaseManager;
    public static void Main(string[] args)
    {
        databaseManager = new MongoDBManager("mongodb+srv://keith:<password>@clusterdev.jgsxl.mongodb.net/?retryWrites=true&w=majority");
    }
}