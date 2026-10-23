namespace CrateWatch.Shops.Services;

public class InvalidShopException(string message) : Exception(message);

public class ScanInProgressException() : Exception("A scan is already running.");
