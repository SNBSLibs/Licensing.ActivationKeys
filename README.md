# Licensing library from SNBSLibs

Free. Easy to use. No license files. **Everything is XML-documented in the code, so IntelliSense can show you all info about any member.**

If you need to know about all aspects of using this library, or you want to test it, consider the branch `tests` containing the unit tests created for this library.

## Examples of usage

### Using `LicensingClient`

Say, we have an app that isn't completely free, and we want to sell licenses for it and activate it through activation keys.

1. First, we need a database to store licenses. This library supports MS SQL Server and MySQL. You may use any database hosting from Windows Azure to [FreeSQLDatabase](https://freesqldatabase.com) (uses MySQL 5.0.12). **Not an advertisement**. Get a connection string and go to the next step.

2. Then we need to start a `LicensingClient` in the main method. It will decide whether to run the full app version or a message "Not licensed".

```c#
using SNBS.Licensing;

// ...

public static void Main(string[] args) {
  LicensingClient.Start(
     "YourConnectionString", "YourProductName", false, null,
     client => {
       // Start the full version
     },
     (client, usability) => {
       // What you do when the app isn't licensed
     }
  );
}
```

3. Let's analyze this code. Method `Start` is static. In the first parameter, it takes the connection string to your database. *Please note that if the connection string provided is invalid, an `InvalidConnectionStringException` will be thrown. The problematic connection string will be stored in the exception data under key `ProblematicConnectionString`.* In the second parameter you pass the name of your project — it is used to store the license information in the registry (licenses for different products store in different places).

4. The third parameter is of type `bool`. It specifies whether the `LicensingClient` should try to connect to MySQL (if it's `false`, the client will try to connect to MS SQL Server). If it's `true`, you should also set the fourth parameter (of type `Version?`) to the version of MySQL. If MS SQL Server is used, this parameter should be `null`. If the third parameter is `true`, but the fourth one is `null`, an `ArgumentException` is thrown.

5. The third parameter has type `Action<LicensingClient>` and is ran when your product has a valid license. The `LicensingClient` instance passed to it can be used to fetch the license, reactivate/deactivate your product and validate activation keys (without using them).

6. The fourth parameter has type `Action<LicensingClient, LicenseUsability>` and is ran where there's no license or an invalid license (configured in the registry for the current product). The `LicensingClient` passed can be used for the same things as described in paragraph 4. **`LicenseUsability` is an enumeration describing reasons why a license is usable/not usable.** Its values are: `Usable`, `Expired`, `NotFound`, `TooManyDevices` (each license can be used by a limited number of devices, set when it was created) and `NoConfiguredLicense`. They should be intuitive. (The difference between `NotFound` and `NoConfiguredLicense` — `NotFound` means a license is configured, but it doesn't exist in the license database. `NoConfiguredLicense` means there's *no license at all*.)

This was the most common usage of the library, but there are other ways, e.g. you can create a `LicensingClient` yourself (specify connection string, product name, use MySQL or not and the version of MySQL, as in the previous example):

```c#
var client = new LicensingClient("YourConnectionString", "YourProductName", false, null);
var usability = client.GetCurrentLicense().Usability;

if (usability != LicenseUsability.Usable) {
  ShowMessage("Your license " +
    (usability == LicenseUsability.Expired) ? "has expired" :
    (usability == LicenseUsability.NotFound) ? "was canceled" :
    (usability == LicenseUsability.NoConfiguredLicense) ? "configuration was corrupted" :
    "was corrupted");
}
```

When you create a `LicensingClient` using the constructor, it automatically connects to the licenses database. Method `GetCurrentLicense()` retrieves the currently used activation key (stored in the registry) and looks up in the database to verify it. The returned type is **structure `LicenseInfo`**. It should be obvious that it contains detailed information about one license. Its properties are:

 - `Key` of type `string`;
 - `Expiration` of type `DateTime` (only date is stored, `DateTime` instead of `DateOnly` was used because of the Entity Framework's mapping mechanism);
 - `Type` of type `LicenseType` (**enumeration containing values `Trial`, `General`, `Professional`**);
 - `Usability` of type `LicenseUsability`.
 
#### Applying activation keys
 
Let's improve the previous example. Generally, applications should ask the end user to activate them if the current license is not usable. The corresponding method of `LicensingClient` is called `ActivateProduct()`. It returns `LicenseInfo` containing the information about the newly activated license (of course, it's activated only if it's usable)

*Please note that the current activation key is stored in the registry, and thus `ActivateProduct()` needs admin permissions. If they aren't provided, a `RegistryAccessException` will be thrown. The inaccessible registry key will be stored in the exception data under key `InaccessibleRegistryKey`.*
 
 ```c#
var client = new LicensingClient("YourConnectionString", "YourProductName");
var usability = client.GetCurrentLicense().Usability;

if (usability != LicenseUsability.Usable) {
  ShowMessage("Your license " +
    (usability == LicenseUsability.Expired) ? "has expired" :
    (usability == LicenseUsability.NotFound) ? "was canceled" :
    (usability == LicenseUsability.NoConfiguredLicense) ? "configuration was corrupted" :
    "was corrupted");
    
  string key = AskUser("Enter an activation key");
  var info = client.ActivateProduct(key);
  
  if (info.Usability == LicenseUsability.Usable) {
    ShowMessage("License successfully activated! Expires at " + info.Expiration.ToShortDateString());
  } else {
    ShowMessage("An error occurred when trying to activate. The license" +
      (usability == LicenseUsability.Expired) ? "has expired" :
      (usability == LicenseUsability.NotFound) ? "was canceled" :
      (usability == LicenseUsability.NoConfiguredLicense) ? "configuration was corrupted" :
      "was corrupted");
  }
}
```
 
A less common method is `ValidateLicense(string key)` which retrieves `LicenseInfo` of the license with the passed key, but doesn't try to activate it on the current device.

There are other (non-common used) members documented as XML in the code. (See also the unit tests from branch `tests`. They are a good documentation.)

### Using `LicensingAdmin`

It's good when we can validate and apply licenses, but a class that would perform CRUD (Create, Read, Update, Delete) operations is also needed. It is `LicensingAdmin`. Its instances can be created just like `LicensingClient` instances, but without specifying product name and without a `Start` method.

1. Let's create a license.

```c#
var admin = new LicensingAdmin("YourConnectionString", false, null);
var info = admin.CreateLicense(DateTime.Today.AddDays(20), LicenseType.Trial, 1);
ShowMessage("The newly created license is " + info.Key);
```

2. Analysis. Method `CreateLicense()` receives three parameters. The first one is the type of the needed license (a value of the `LicenseType` enumeration). The second one is a `DateTime` object representing the expiration date. The third one is the maximum number of devices (`short`) that can use the license.

3. The returned object is `LicenseInfo` representing the new license. The most common use in this case is taking the (randomly generated) key of the new license.

Of course, `LicensingAdmin` can also update and delete licenses.

```c#
var admin = new LicensingAdmin("YourConnectionString", false, null);
var info = admin.UpdateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA", null, LicenseType.Professional, 10);

ShowMessage("The license " + info.Key + " is now of type " + info.Type.ToString() + " and can be used by (maximum) " + info.MaxDevices.ToString() + " devices.");
```

Method `UpdateLicense` receives four arguments. The first one is key of the license to update.

The second, third and fourth parameters are, correspondingly, `DateTime?` (expiration), `LicenseType?` (type) and `short?` (maximum number of devices using the license). They are the new license parameters. If you want to leave a parameter as it was, pass `null` in the corresponding argument.

The returned type is `LicenseInfo` that stores updated information about the license.

Let's imagine we no longer need the updated license. It should be deleted:

```c#
var admin = new LicensingAdmin("YourConnectionString", false, null);
var info = admin.DeleteLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

ShowMessage("The license " + info.Key + " was deleted.");
```

The only argument is key of the license to delete. The returned type is `LicenseInfo` storing information about the deleted license.
