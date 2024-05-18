using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using WebApi.Di;

namespace WebApi;

public class Program {

   static void Main(string[] args) {
      
      // WebApplication Builder Pattern
      // #####################################################################
      var builder = WebApplication.CreateBuilder(args);
      
      // Configure logging
      // ---------------------------------------------------------------------
      builder.Logging.ClearProviders();
      builder.Logging.AddConsole();
      builder.Logging.AddDebug();
      // Write Debug Logging into a file
      // var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      // var path = Path.Combine(directory,"WebApi");
      // var tracePath = Path.Join(path, $"LogWebApi_{DateTime.Now.ToString("yyyy_MM_dd-HHmmss")}.txt");
      // Trace.Listeners.Add(new TextWriterTraceListener(File.Create(tracePath)));
      // Trace.AutoFlush = true;
      
      
      // Configure DI-Container aka builder.Services:IServiceCollection
      // ---------------------------------------------------------------------
      // add http logging 
      builder.Services.AddHttpLogging(opts =>
         opts.LoggingFields = HttpLoggingFields.All);
      // add Controllers
      builder.Services.AddControllers()
         .AddJsonOptions(opt => {
            // configure Json serialization
            opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
         });
      // add Core
      builder.Services.AddCore();
      // add Persitence
      builder.Services.AddPersistence(builder.Configuration);
      
      // Add Error handling
      builder.Services.AddProblemDetails();

      // API Versioning
      // ---------------------------------
      var apiVersionReader = ApiVersionReader.Combine(
         new UrlSegmentApiVersionReader(),
         new HeaderApiVersionReader("x-api-version")
      // new MediaTypeApiVersionReader("x-api-version"),
      // new QueryStringApiVersionReader("api-version")
      );

      builder.Services.AddApiVersioning(opt=> {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true;
//          opt.ApiVersionReader = new UrlSegmentApiVersionReader();
            opt.ApiVersionReader = apiVersionReader;
         })
         .AddMvc()
         .AddApiExplorer(options => {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
         });
      
      // Add OpenApi/Swagger documentatio
      // ---------------------------------------------------------------------
      builder.Services.AddSwaggerGen(opt => {
         // Configure Swagger to use the xml documentation file
         //  var xmlFile = Path.ChangeExtension(typeof(Program).Assembly.Location, ".xml");
         //  opt.IncludeXmlComments(xmlFile);
         var dir = new DirectoryInfo(AppContext.BaseDirectory);
         // combine WebApi.Controllers.xml and WebApi.Core.xml
         foreach (var file in dir.EnumerateFiles("*.xml")) {
            opt.IncludeXmlComments(file.FullName);
         }
      });
      builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
      
      // Build the WebApplication
      // #####################################################################
      var app = builder.Build();
      // use http logging
      app.UseHttpLogging();
      // error endpoints
      if (app.Environment.IsDevelopment())
         app.UseExceptionHandler("/banking/error-development");     
      else     
         app.UseExceptionHandler("/banking/error");
      
      // API Versioning, OpenAPI/Swagger documentation
      var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
      if(app.Environment.IsDevelopment()){
         app.UseSwagger();
         app.UseSwaggerUI(options => {
            foreach(var description in provider.ApiVersionDescriptions){
               options.SwaggerEndpoint(
                  $"/swagger/{description.GroupName}/swagger.json",
                  description.GroupName.ToUpperInvariant());
            }
         });
      }
      
      // routing
      app.MapControllers();
      // Run the WebApplication
      app.Run();

   }
}