# JsonCommands

JsonCommands is a web API plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It automatically provides **RESTful JSON web service** for all entities, actions and other data structures that are defined in a Rhetos application.

It is intended to be used on an ASP.NET application that contains or references the Rhetos domain object model.

See [rhetos.org](http://www.rhetos.org/) for more information on Rhetos.

1. [Features](#features)
   1. [General rules](#general-rules)
   2. [Reading data](#reading-data)
   3. [Writing data](#writing-data)
   4. [Actions](#actions)
   5. [Reports](#reports)
   6. [Obsolete features](#obsolete-features)
2. [Examples](#examples)
3. [Developing client applications](#developing-client-applications)
4. [Installation](#installation)
   1. [Configure legacy JSON format](#configure-legacy-json-format)
5. [HTTPS](#https)
6. [Adding Swagger/OpenAPI](#adding-swaggeropenapi)
7. [How to contribute](#how-to-contribute)
   1. [Building and testing the source code](#building-and-testing-the-source-code)

## Features

### General rules

1. Any POST request should contain a header: `Content-Type: application/json; charset=utf-8`

Examples in this article will assume that your application's base URI is `https://localhost:5000`.

Response:

* The response status code will indicate the success of the request:
  * 200 - OK,
  * 4xx - client error (incorrect data or request format, authentication or authorization error),
  * 500 - internal server error.
* In case of an error, the response body will contain more information on the error. It is a JSON object with properties:
  * UserMessage - a message to be displayed to the user.
  * SystemMessage - additional error metadata for better client UX
    (for example, a property that caused an error).

Following are URI templates for the web methods.

### Writing data

Send a POST request to `https://localhost:5000/jc/write`
The post request should have a following format:
```json
[
  {
    "Bookstore.Book": {
      "Delete": [
        { "ID": "00a7302a-df84-43a4-8c1c-6f7aa13c63b4" }
      ],
      "Update": [
        { "ID": "8faa49db-aa6a-4e0c-9459-c1a16826ffc5", "Title": "Some other book" },
        { "ID": "9e76a291-a76f-43e3-85ba-60bb88c3900b", "Title": "Yet another book" }
      ],
      "Insert": [
        { "ID": "ed609ccf-346e-423d-9e21-145571dbaee9", "Title": "The Art of Computer Programming" }
      ]
    }
  },
  {
    "Bookstore.Comment": {
      "Insert": [
        { "Text": "Very interesting", "BookID": "ed609ccf-346e-423d-9e21-145571dbaee9" },
        { "Text": "Educational", "BookID": "ed609ccf-346e-423d-9e21-145571dbaee9" }
      ]
    }
  }
]
```

For each entity write block (for example for Bookstore.Book above), internally Rhetos will always execute delete operation first, then update, then insert. If a custom order is needed within one write command, the client can control it by using multiple blocks for same entity. For example:

```json
[
  {  "Bookstore.Book": { "Insert": [...] } },
  {  "Bookstore.Book": { "Delete": [...] } },
  {  "Bookstore.Book": { "Update": [...] } }
]
```

## Developing client applications

When developing client applications, use standard JSON serialization and URL encoding helpers
to generate URL query string parameters for the REST web requests.
It is recommended to use common libraries for REST requests, such as **RestSharp** for .NET applications.

For example, when generating `filters` parameter for GET request,
avoid generating URL query string manually.
It would provide opportunity for errors with certain characters
that cannot be directly written in JSON or URL,
they must be escaped with prefix character or encoded in hex format.

The following example demonstrates an expected format of URL query parameters,
by using **Newtonsoft.Json** for JSON serialization
and standard .NET Framework class UrlEncode.

```cs
using Newtonsoft.Json;
using System;
using System.Net;
namespace JsonUrlEncoded
{
    class Program
    {
        static void Main(string[] args)
        {
            var myCustomFilter = new
            {
                Filter = "MyCustomFilter",
                Value = new
                {
                    Text = @"Characters\/""?",
                    DateFrom = DateTime.Now,
                    OwnerID = Guid.NewGuid(),
                    File = new byte[] { 1, 2, 3 }
                }
            };
            var filters = new[] { myCustomFilter };

            var jsonSettings = new JsonSerializerSettings();
            // If needed, configure Newtonsoft.Json for backward-compatibility with older versions of JsonCommands v1-v4:
            // 1. legacy Microsoft DateTime serialization,
            // 2. byte[] serialization as JSON array of integers instead of Base64 string.
            //jsonSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
            //jsonSettings.Converters.Add(new Rhetos.Host.AspNet.RestApi.Utilities.ByteArrayConverter()); // Rhetos.JsonCommands NuGet
            
            string json = JsonConvert.SerializeObject(filters, jsonSettings);
            string urlQuery = WebUtility.UrlEncode(json);
            Console.WriteLine($"JSON: {json}");
            Console.WriteLine($"URL query: ?filters={urlQuery}");
        }
    }
}
```

Note that URL query encoding should be skipped when sending parameters in request body (POST and PUT),
or if using a REST library that will automatically encode URL query parameters for each request
(**RestSharp**, for example).

## Installation

Installing this package to a Rhetos web application:

1. Add 'Rhetos.JsonCommands' NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.
2. Extend Rhetos services configuration (at `services.AddRhetosHost`) with the REST API:
   ```cs
   .AddRestApi(o =>
   {
       o.BaseRoute = "rest";
   });
   ```

### Configure legacy JSON format

If needed, configure legacy JSON format for compatibility with existing applications and plugins (v1-v4):

* A) Properties starting with uppercase in JSON objects:
  ```cs
  // Backward-compatibility with older versions of JsonCommands
  services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
  ```
* B) For full backward compatibility, add **Microsoft.AspNetCore.Mvc.NewtonsoftJson** NuGet package,
  and the following code to Startup.ConfigureServices method:
  ```cs
  services.AddControllers()
      .AddNewtonsoftJson(o =>
      {
          // Using NewtonsoftJson for backward-compatibility with older versions of JsonCommands:
          // 1. Properties starting with uppercase in JSON objects.
          o.UseMemberCasing();
          // 2. Legacy Microsoft DateTime serialization.
          o.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
          // 3. byte[] serialization as JSON array of integers instead of Base64 string.
          o.SerializerSettings.Converters.Add(new Rhetos.Host.AspNet.RestApi.Utilities.ByteArrayConverter());
      });
  ```

## HTTPS

To enable HTTPS, follow the instructions in [Set up HTTPS](https://github.com/Rhetos/Rhetos/wiki/Setting-up-Rhetos-for-HTTPS).

## Adding Swagger/OpenAPI

If not already included, add Swashbuckle to your ASP.NET Core application, see instructions: [Get started with Swashbuckle and ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-5.0&tabs=visual-studio).

Add support for multiple entities with the same name in different modules:

1. By default, Swashbuckle will return "Failed to load API definition." error, it the same type name occurs in different namespaces. To fix this, in Startup.ConfigureServices method, inside `services.AddSwaggerGen` method call add `c.CustomSchemaIds(type => type.ToString()); // Allows multiple entities with the same name in different modules`.
For more info see "Conflicting schemaIds" in the [Swagger documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#customize-schema-ids).

Show Rhetos REST API in the Swagger UI:

1. In Startup.ConfigureServices method, in `.AddRestApi` method call,
   add `o.GroupNameMapper = (conceptInfo, controller, oldName) => "rhetos";`.
2. In Startup.ConfigureServices method, in `.AddSwaggerGen` method call,
   add `c.SwaggerDoc("rhetos", new OpenApiInfo { Title = "Rhetos REST API", Version = "v1" });`.
3. In Startup.Configure method add, in `.UseSwaggerUI` method call,
   add `c.SwaggerEndpoint("/swagger/rhetos/swagger.json", "Rhetos REST API");`.
   If there are multiple swagger endpoints configured here, **place this one first** if you want to open it by default.

As an alternative, you can show Rhetos REST API **split into multiple** Swagger documents (pages) to improve load time of the Swagger UI for large projects.

1. Specify document names in Rhetos REST API:
   1. Option A) If you want to have one Swagger documents *for each DSL module*,
      remove any code from Startup.cs that sets `GroupNameMapper`.
      DSL Module name is used for grouping by default.
   2. Option B) If you want to specify custom Swagger documents, in Startup.ConfigureServices method, in `.AddRestApi` method call,
      add `o.GroupNameMapper = (conceptInfo, controller, oldName) =>  ... return document name for each conceptInfo ...`.
      Implement the custom delegate here, that will result with different Swagger document names based on `conceptInfo` parameter.
2. For each document name specified above (each DSL Module, e.g.), add the following code and replace `MyModuleName` with the document name accordingly (it is case sensitive).
   1. In Startup.ConfigureServices method, in `.AddSwaggerGen` method call,
      add `c.SwaggerDoc("MyModuleName", new OpenApiInfo { Title = "MyModuleName REST API", Version = "v1" });`.
   2. In Startup.Configure method add, in `.UseSwaggerUI` method call,
      add `c.SwaggerEndpoint("/swagger/MyModuleName/swagger.json", "MyModuleName REST API");`.
      If there are multiple swagger endpoints configured here,  **place at the first position** the one that you want to open by default.
   3. For example, see lines with `SwaggerDoc` and `SwaggerEndpoint` in [Bookstore Startup.cs](https://github.com/Rhetos/Bookstore/blob/baa33901c71224d13e5bae2c8312f34cd759428e/src/Bookstore.Service/Startup.cs), for modules Bookstore, Common, AuthenticationDemo and DemoRowPermissions2.

## How to contribute

Contributions are very welcome. The easiest way is to fork this repo, and then
make a pull request from your fork. The first time you make a pull request, you
may be asked to sign a Contributor Agreement.
For more info see [How to Contribute](https://github.com/Rhetos/Rhetos/wiki/How-to-Contribute) on Rhetos wiki.

### Building and testing the source code

* Note: This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
  You don't need to build it from source in order to use it in your application.
* To build the package from source, run `Clean.bat`, `Build.bat` and `Test.bat`.
* For the test script to work, you need to create an empty database and
  a settings file `test\TestApp\ConnectionString.local.json`
  with the database connection string (configuration key "ConnectionStrings:RhetosConnectionString").
* The build output is a NuGet package in the "Install" subfolder.
