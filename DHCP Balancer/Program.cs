using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Microsoft.Extensions.Configuration;

class Program
{
    class DhcpScope
    {
        public string Id { get; set; }
        public string Gateway { get; set; }
        public List<Reservation> Reservations { get; set; }
    }

    class Reservation
    {
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    static void Main()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var scopes = config.GetSection("DhcpSettings:Scopes").Get<List<DhcpScope>>();

        using (PowerShell psInstance = PowerShell.Create())
        {
            psInstance.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -Force");
            psInstance.AddScript("Import-Module DhcpServer");

            foreach (var scope in scopes)
            {
                if (!string.IsNullOrEmpty(scope.Gateway))
                {
                    psInstance.AddScript($"Set-DhcpServerv4OptionValue -ScopeId {scope.Id} -OptionId 3 -Value {scope.Gateway}");
                }

                if (scope.Reservations != null && scope.Reservations.Count > 0)
                {
                    foreach (var reservation in scope.Reservations)
                    {
                        psInstance.AddScript($"Add-DhcpServerv4Reservation -ScopeId {scope.Id} -IPAddress {reservation.IpAddress} -ClientId {reservation.MacAddress} -Description '{reservation.Description}' -Name '{reservation.Name}' -ErrorAction SilentlyContinue");
                    }
                }
            }

            try
            {
                var results = psInstance.Invoke();

                if (psInstance.HadErrors)
                {
                    Console.WriteLine("Error while importing DhcpServer module or updating scope and reservation settings.");
                    foreach (var error in psInstance.Streams.Error)
                    {
                        Console.WriteLine(error.Exception.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Scope and reservation settings updated successfully.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
            finally
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }
    }
}
