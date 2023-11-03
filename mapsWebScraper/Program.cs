using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Collections.Generic;
using System.IO;

class Program
{

    private static bool canScroll = true; // Declare canScroll at the class level

    static void Main()
    {
        // Specify the URL you want to fetch.
        //string targetUrl = "https://www.google.com/maps/search/%CF%80%CF%81%CE%B1%CF%84%CE%AE%CF%81%CE%B9%CE%B1+%CE%B8%CE%95%CF%83%CF%83%CE%B1%CE%BB%CE%BF%CE%BD%CE%AF%CE%BA%CE%B7/@40.622081,22.950048,13z/data=!3m1!4b1?ucbcb=1&entry=ttu";

        string searchTerm = "καφέ";
        string location = "νομός θεσσαλονίκης";
        string targetUrl = "https://www.google.com/maps/search/" + searchTerm + " " + location + "/@40.622081,22.950048,10z/data=!3m1!4b1?ucbcb=1&entry=ttu";
        string htmlContent = "";

        // Set up the ChromeDriver
        var options = new ChromeOptions();
        // options.AddArgument("--headless"); // Run Chrome in headless mode (without GUI)

        using (var driver = new ChromeDriver(options))
        {
            try
            {
                // Navigate to the target URL
                driver.Navigate().GoToUrl(targetUrl);

                // Wait for the page to load completely (you may need to adjust the timeout)
                var timeout = TimeSpan.FromSeconds(30);
                var wait = new WebDriverWait(driver, timeout);
                wait.Until(d => d.Title.Contains("Google"));

                IWebElement leftList = driver.FindElement(By.CssSelector(".w6VYqd"));

                // Scroll down until you can't scroll anymore
                canScroll = true;
                Actions actions = new Actions(driver);

                Timer timeoutTimer = new Timer(25000); // 60000 milliseconds = 60 seconds (adjust to your desired timeout)
                timeoutTimer.Elapsed += OnTimeoutElapsed;
                timeoutTimer.AutoReset = false; // Set to false to run the timer only once
                timeoutTimer.Enabled = true; // Start the timer


                while (canScroll)
                {
                    try
                    {
                        actions.MoveToElement(leftList).SendKeys(Keys.PageDown).Perform();

                        if (canScroll)
                        {
                            Console.WriteLine("Scrolling...");
                        }
                        else
                        {
                            Console.WriteLine("No more scrolling!");
                        }
                    }
                    catch (OpenQA.Selenium.ElementNotInteractableException)
                    {
                        canScroll = false;
                    }
                }

                // Get the page source (HTML content) after it has loaded
                htmlContent = driver.PageSource;

                if (!string.IsNullOrEmpty(htmlContent))
                {
                    Console.WriteLine("HTML content successfully fetched.");

                    // Now, you can proceed with your parsing and processing of the HTML content.

                    ParseHTML(htmlContent, searchTerm, location);

                    // Console.WriteLine(htmlContent);

                    Console.WriteLine("Press any key to close the browser...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("HTML content is empty or not fetched.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }

    static void OnTimeoutElapsed(object sender, ElapsedEventArgs e)
    {
        canScroll = false; // Stop the scrolling loop
        Console.WriteLine("Timeout elapsed. Stopping scrolling.");
    }

    static void ParseHTML(string htmlContent, string searchTerm, string location)
    {


        List <String> rawData = new List<String>();
        // The strings you want to search for. 
        
        string delim2 = "class=\"hfpxzc\"";
        string delim = delim2;
        int delim_index = htmlContent.IndexOf(delim, StringComparison.OrdinalIgnoreCase);
        

        if (delim_index >= 0)
        {
            int currentIndex = 0;
            int count = 0;
            int delimiterIndex;

            while ((delimiterIndex = htmlContent.IndexOf(delim, currentIndex)) != -1)
            {
                string substring = htmlContent.Substring(delimiterIndex, 800);
                Console.WriteLine(substring); // Print the corresponding substring
                rawData.Add(substring);
                Console.WriteLine("\n\nAdded text to list\n\n");
                count++;
                currentIndex = delimiterIndex + delim.Length;
            }

            Console.WriteLine("Count: " + count.ToString());
        }
        else
        {
            Console.WriteLine(delim + " does not exist in the response");
        }

        if(rawData.Count >= 0)
        {
            CleanRawData(rawData, searchTerm, location);
        }
    } //End of ParseHTML

    static void CleanRawData(List<String> list, string searchTerm, string location)
    {
        List<Entry> entries = new List<Entry>();

        Console.WriteLine("Extracting data...");

        string label_ref_start = "aria-label=\"";
        string label_ref_end= "\" href=";

        string lat_ref_start = "!3d4";

        string long_ref_start = "!4d2";

        foreach (String s in list)
        {
            // Extracting label
            int label_start = s.IndexOf(label_ref_start) + label_ref_start.Length;
            int label_end = s.IndexOf(label_ref_end);
            string label = s[label_start..label_end];

            //Console.WriteLine("Label :" + label);

            // Extracting lat 
            int lat_start = s.IndexOf(lat_ref_start) + lat_ref_start.Length - 1;
            int lat_end = s.IndexOf(long_ref_start);
            string latitude = s[lat_start..lat_end];

            //Console.WriteLine("Latitude :" + latitude);

            // Extracting long
            int long_start = s.IndexOf(long_ref_start) + long_ref_start.Length - 1;
            int long_end = s.IndexOf('!', long_start);
            string longitude = s[long_start..long_end];
          
            //Console.WriteLine("Longitude :" + longitude);

            //Add new object to the entries list
            entries.Add(new Entry(label, latitude, longitude));
        }

        //Create output stream
        string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string fileName = (searchTerm + "-" + location).ToUpper() + ".csv";
        string filePath = Path.Combine(rootDirectory, fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {

                foreach (Entry s in entries)
                {
                    writer.WriteLine("\"" + s.label + "\"" + "," + s.latitude + "," + s.longitude);
                    Console.WriteLine("\"" + s.label + "\"" + "," + s.latitude + "," + s.longitude);
                }

                }
                Console.WriteLine("Content has been written to " +fileName + " at " + filePath);
            }
            catch (IOException e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }        
    }
}


public class Entry
{
    public string label;
    public string latitude;
    public string longitude;
    
    public Entry(string label, string latitude, string longitude)
    {
        this.label = label;
        this.latitude = latitude;
        this.longitude = longitude;
    }
}