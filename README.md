# Licensing library from SNBS

Free. Easy to use. No license files. **Everything is XML-documented in the code, so IntelliSense can show you all info about any member.**

If you need to know about all aspects of using this library, or you want to test it, consider the directory `Licensing.ActivationKeys.Tests` containing the unit tests created for this library.

## Installation

The easiest way to install this library is using NuGet. Right-click on your project name is Visual Studio's Solution Explorer, select option "Manage NuGet packages", find package "Licensing.ActivationKeys" and install it. Or use the `Install-Package` command:

```powershell
Install-Package SNBS.Licensing.ActivationKeys
```

You can also clone this repository (or download it using the green "Code" button), compile it and add a reference to the compiled assembly in your project.

## Examples of usage

### Using `LicensingClient`

Say, we have an app that isn't completely free, and we want to sell licenses for it and activate it through activation keys.

1. First, we need a database to store licenses. This library supports MS SQL Server and MySQL. You may use any database hosting from Windows Azure to [FreeSQLDatabase](https://freesqldatabase.com) (uses MySQL 5.0.12). **Not an advertisement. After creating your database will of course be empty, but this library will automatically set it up.** Get a connection string and go to the next step.

2. Then we need to start a `LicensingClient` in the main method. It will decide whether to run the full app version or a message "Not licensed". *Please note that `LicensingClient` opens a registry key in the constructor, and thus it needs admin permissions. If they aren't provided, a `RegistryAccessException` will be thrown. The inaccessible registry key will be stored in the exception data under key `InaccessibleRegistryKey`.*

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

3. Let's analyze this code. Method `Start` is static. In the first parameter, it takes the connection string to your database. *Please note that if the connection string provided is invalid, or the database structure is invalid (the valid structure is above), a `DatabaseException` will be thrown.* In the second parameter you pass the name of your project — it is used to store the license information in the registry (licenses for different products store in different places).

4. The third parameter is of type `bool`. It specifies whether the `LicensingClient` should try to connect to MySQL (if it's `false`, the client will try to connect to MS SQL Server). If it's `true`, you should also set the fourth parameter (of type `Version?`) to the version of MySQL. If MS SQL Server is used, this parameter should be `null`. If the third parameter is `true`, but the fourth one is `null`, an `ArgumentException` is thrown.

5. The third parameter has type `Action<LicensingClient>` and is ran when your product has a valid license. The `LicensingClient` instance passed to it can be used to fetch the license, reactivate/deactivate your product and validate activation keys (without using them).

6. The fourth parameter has type `Action<LicensingClient, LicenseUsability>` and is ran where there's no license or an invalid license (configured in the registry for the current product). The `LicensingClient` passed can be used for the same things as described in paragraph 4. **`LicenseUsability` is an enumeration describing reasons why a license is usable/not usable.** Its values are: `Usable`, `Expired`, `NotFound`, `TooManyDevices` (each license can be used by a limited number of devices, set when it was created) and `NoConfiguredLicense`. They should be intuitive. (The difference between `NotFound` and `NoConfiguredLicense` — `NotFound` means a license is configured, but it doesn't exist in the license database. `NoConfiguredLicense` means there's *no license at all*.) *Note that the value `NoConfiguredLicense` cannot be returned by any method, except `GetCurrentLicense()`.*

This was the most common usage of the library, but there are other ways, e.g. you can create a `LicensingClient` yourself (specify connection string, product name, use MySQL or not and the version of MySQL, as in the previous example):

```c#
using (var client = new LicensingClient("YourConnectionString", "YourProductName", false, null)) {
  var usability = client.GetCurrentLicense().Usability;

  if (usability != LicenseUsability.Usable) {
    ShowMessage("Your license " +
      (usability == LicenseUsability.Expired) ? "has expired" :
      (usability == LicenseUsability.NotFound) ? "was canceled" :
      (usability == LicenseUsability.NoConfiguredLicense) ? "configuration was corrupted" :
      "was corrupted");
  }
}
```

When you create a `LicensingClient` using the constructor, it automatically connects to the licenses database. Method `GetCurrentLicense()` retrieves the currently used activation key (stored in the registry) and looks up in the database to verify it. The returned type is **structure `LicenseInfo`**. It should be obvious that it contains detailed information about one license. Its properties are:

 - `Key` of type `string?`;
 - `Expiration` of type `DateTime?` (only date is stored, `DateTime` instead of `DateOnly` was used because of the Entity Framework's mapping mechanism);
 - `Type` of type `LicenseType?` (**enumeration containing values `Trial`, `General`, `Professional`**);
 - `Usability` of type `LicenseUsability` (all other properties will be `null` if this one isn't equal to `LicenseUsability.Usable`).
 
#### Applying activation keys
 
Let's improve the previous example. Generally, applications should ask the end user to activate them if the current license is not usable. The corresponding method of `LicensingClient` is called `ActivateProduct()`. It returns `LicenseInfo` containing the information about the newly activated license (of course, it's activated only if it's usable).

 ```c#
using (var client = new LicensingClient("YourConnectionString", "YourProductName")) {
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
      ShowMessage("An error occurred when trying to activate. The license " +
        (info.Usability == LicenseUsability.Expired) ? "has expired" :
        (info.Usability == LicenseUsability.NotFound) ? "was canceled" :
        (info.Usability == LicenseUsability.TooManyDevices) ? "was used by too many devices" :
        "was corrupted");
    }
  }
}
```

There are other (non-common used) members documented as XML in the code. (See also the unit tests from directory `Licensing.ActivationKeys.Tests`. They are a good documentation.)

### Using `LicenseValidator`

`LicenseValidator` can be used to retrieve information (`LicenseInfo`) about a license, without trying to apply it on the current device. Its only method is `ValidateLicense` (except methods of `System.Object`).

The usage isn't complicated.

1. Let's validate a license. I will use MySQL here, to show this feature:

```c#
using (var validator = new LicenseValidator("Server=sql7.freesqldatabase.com; Database=sql7594998; Uid=sql7594998; Pwd=l2TZZAQ5hB", true,
  new Version(5, 0, 12))) {
  var info = validator.ValidateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

  if (info.Usability == LicenseUsability.Usable) {
    ShowMessage("This license is valid");
  }
}
```

3. If you run the code above, it will connect to my own database hosted on [FreeSQLDatabase](https://freesqldatabase.com). **Not an advertisement. This service is used because it's free and easy to use.** If you have your own database hosted there, replace values in the connection string with the values in email they will send you.

4. `LicenseValidator` doesn't deal with registry, so admin permissions aren't needed.

5. The constructor takes connection string to licenses database, a `bool` value telling whether to use MySQL and the version of MySQL (if it is used), just like the `LicensingClient` constructor, but without specifying product name.

6. Method `ValidateLicense` takes a license key in its only argument and returns `LicenseInfo` representing that license.

### Using `LicensingAdmin`

It's good when we can validate and apply licenses, but a class that would perform CRUD (Create, Read, Update, Delete) operations is also needed. It is `LicensingAdmin`. Its instances can be created just like `LicensingClient` instances, but without specifying product name and without a `Start` method.

1. Let's create a license.

```c#
using (var admin = new LicensingAdmin("YourConnectionString", false, null)) {
  var info = admin.CreateLicense(DateTime.Today.AddDays(20), LicenseType.Trial, 1);
  
  ShowMessage("The newly created license is " + info.Key);
}
```

2. Analysis. Method `CreateLicense()` receives three parameters. The first one is the type of the needed license (a value of the `LicenseType` enumeration). The second one is a `DateTime` object representing the expiration date. The third one is the maximum number of devices (`short`) that can use the license.

3. The returned object is `LicenseInfo` representing the new license. The most common use in this case is taking the (randomly generated) key of the new license.

Of course, `LicensingAdmin` can also update and delete licenses.

```c#
using (var admin = new LicensingAdmin("YourConnectionString", false, null)) {
  var info = admin.UpdateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA", null, LicenseType.Professional, 10);

  ShowMessage("The license " + info.Key + " is now of type " + info.Type.ToString() + " and can be used by (maximum) " + info.MaxDevices.ToString() + " devices.");
}
```

Method `UpdateLicense` receives four arguments. The first one is key of the license to update.

The second, third and fourth parameters are, correspondingly, `DateTime?` (expiration), `LicenseType?` (type) and `short?` (maximum number of devices using the license). They are the new license parameters. If you want to leave a parameter as it was, pass `null` in the corresponding argument.

The returned type is `LicenseInfo` that stores updated information about the license.

Let's imagine we no longer need the updated license. It should be deleted:

```c#
using (var admin = new LicensingAdmin("YourConnectionString", false, null)) {
  var info = admin.DeleteLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

  ShowMessage("The license " + info.Key + " was deleted.");
}
```

The only argument is key of the license to delete. The returned type is `LicenseInfo` storing information about the deleted license.

### Casts

Classes `LicensingClient`, `LicenseValidator` and `LicensingAdmin` deal with the same database, but offer different functions. That's why I've defined explicit casts between them:

```c#
using (var admin = new LicensingAdmin("YourConnectionString", false, null)) {
  // Use it
  
  var validator = (LicenseValidator)admin;
  var info = validator.ValidateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");
}
```

You can convert the mentioned classes to each other however you like, but you cannot convert `LicenseValidator` to `LicensingClient` and `LicensingAdmin` to `LicensingClient` (because `LicensingClient` needs product name for creation, which cannot be taken from other classes). But you can use `LicensingClient` constructors for that:

```c#
var admin = new LicensingAdmin("YourConnectionString", false, null);
var client = new LicensingClient(admin, "YourProductName");
```

The code above will retrieve the connection string and information about usage of MySQL from the `LicensingAdmin` instance (you can also pass a `LicenseValidator` instance).

### Disposing

**It is *very* important to dispose `LicensingClient`, `LicenseValidator` and `LicensingAdmin` after use** because the connection to the licenses database is closed only when `Dispose()` is called. As the mentioned classes implement `IDisposable`, you can use the `using` operator instead of calling `Dispose()` yourself. 

## Tests

The unit tests need an empty LocalDB database. First, let this library create it, then run the following query to add a test record:

```sql
INSERT INTO [Licenses] ([Key],
                        [Expiration],
                        [Type],
                        [UsingDevices],
                        [MaxDevices])
VALUES ('AAAAA-AAAAA-AAAAA-AAAAA-AAAAA',
        '2024-12-30',
        'Professional',
        0,
        3);
```

**Unit tests in class `LicensingAdminTests` should be ran one-by-one! Test `CanUpdateLicense` should be ran before test `CanDeleteLicense`, and test `CanCreateLicense` — before test `CanDeleteOldLicenses`!**
