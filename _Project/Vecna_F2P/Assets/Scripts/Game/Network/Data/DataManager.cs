using SQLite;
using System.Threading.Tasks;
using UnityEngine;
public class DataBase
{
    [PrimaryKey]
    public string DeviceId { get; set; }
}
public class PlayerData : DataBase
{
    public int Gold { get; set; }
}

public class DataManager
{
    private const string DATA_PATH = "GameData.db";
    private static SQLiteAsyncConnection _db;
    private static string _deviceId;

    private static async Task InitTable<T>() where T : DataBase, new()
    {
        if (_db == null)
        {
            _deviceId = SystemInfo.deviceUniqueIdentifier;
            string path = System.IO.Path.Combine(Application.persistentDataPath, DATA_PATH);
            _db = new SQLiteAsyncConnection(path);
        }
        await _db.CreateTableAsync<T>();
    }

    public static async Task<T> Get<T>() where T : DataBase, new()
    {
        await InitTable<T>();

        T data = await _db.FindAsync<T>(_deviceId);

        if (data == null)
        {
            data = new T { DeviceId = _deviceId };
        }
        return data;
    }

    public static async Task Save<T>(T data) where T : DataBase, new()
    {
        await InitTable<T>();

        data.DeviceId = _deviceId;

        await _db.InsertOrReplaceAsync(data);
    }
}