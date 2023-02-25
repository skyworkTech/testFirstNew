using Microsoft.Playwright;
using System;
using System.Data.SqlTypes;
using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

string executablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

// 讀取 firstTest.ini 檔案
string iniPath = Path.Combine(executablePath, "firstTest.ini");
var ini = File.ReadAllLines(iniPath)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

string executablePathValue = ini["executablePathValue"];
string userid = ini["userid"];
string findString = ini["findString"];
string targetUrl = ini["targetUrl"];
string notifyBaseUrl = ini["notifyBaseUrl"];

var playwright = await Playwright.CreateAsync();


//const page = await context.newPage();
int count = 0; // 計數器變數
while (true)
{
    // 在這裡執行迴圈內容
    count++;

    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        ExecutablePath = executablePathValue,
        Headless = false,
        Args = new[] { "--start-maximized", "--auto-open-devtools-for-tabs" }
    });

    // 创建一个新的上下文
    var context = await browser.NewContextAsync();
    var page = await context.NewPageAsync();
    try
    {
        // 前往網頁
        await page.GotoAsync(targetUrl);
    }
    catch (TaskCanceledException)
    {
        // 捕捉 TaskCanceledException 異常，表示等待時間已經超過 60 秒
        Console.WriteLine("等待超時");

        //過三秒後傳送螢幕截圖
        await page.WaitForTimeoutAsync(3500);
        // 截取屏幕
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screen.png" });
        await using var stream = System.IO.File.OpenRead("screen.png");

        // 送出訊息圖片
        SendNotificationAsync(notifyBaseUrl, count, userid);
    }
        // 等待兩秒
        await page.WaitForTimeoutAsync(2000);

    // 按下按鈕 等待轉指
    await page.ClickAsync("h6.login");

    // 等待 1.5 秒
    await page.WaitForTimeoutAsync(1500);

    // 取得第一個 input 元素
    var firstInput = await page.QuerySelectorAsync("input");

    // 等待三秒
    await page.WaitForTimeoutAsync(1000);

    // 使用 type 方法將 AAATest 輸入至 input 中
    /// 回車六次 避免不同os keyboard不同
    await firstInput.FocusAsync();
    for (var i = 0; i < 6; i++)
    {
        await page.Keyboard.PressAsync("Backspace");
    }
    //await page.Keyboard.PressAsync("Control+KeyA");
    //await page.Keyboard.PressAsync("Backspace");
    await firstInput.TypeAsync(userid);
    // 等待三秒

    //Console.ReadLine();//

    // 等待三秒
    await page.WaitForTimeoutAsync(2000);

    // 等待 60 秒或直到發生導航事件或點擊事件
    try
    {
        await Task.WhenAny(
            page.WaitForNavigationAsync(),
            page.ClickAsync("button"),
            Task.Delay(30000)
        );
    }
    catch (TaskCanceledException)
    {
        // 捕捉 TaskCanceledException 異常，表示等待時間已經超過 60 秒
        Console.WriteLine("等待超時");

        //過三秒後傳送螢幕截圖
        await page.WaitForTimeoutAsync(3500);
        // 截取屏幕
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screen.png" });
        await using var stream = System.IO.File.OpenRead("screen.png");

        // 送出訊息圖片
        SendNotificationAsync(notifyBaseUrl, count, userid);
    }


    // 按下按鈕 等待轉指
    // await page.ClickAsync("button");

    // 等待三秒
    await page.WaitForTimeoutAsync(8700);

    // 搜尋 h3 標籤
    var h3Elements = await page.QuerySelectorAllAsync("h3");

    var found = false;
    foreach (var element in h3Elements)
    {
        var html = await element.InnerHTMLAsync();
        if (html == findString)
        {
            found = true;
        }
    }

    if (found == false)
    {
        Console.WriteLine(findString + "不見了");

        //過三秒後傳送螢幕截圖
        await page.WaitForTimeoutAsync(3500);
        // 截取屏幕
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screen.png" });
        await using var stream = System.IO.File.OpenRead("screen.png");

        // 送出訊息圖片
        SendNotificationAsync(notifyBaseUrl,count,userid);

        break;
    }
    //如果跑100次 則顯示一下在 console.log
    if (count % 100 == 0)
    {
        Console.WriteLine($"迴圈運行了 {count} 次。");
    }

    await page.CloseAsync();
    await context.CloseAsync();
    await browser.CloseAsync();
}

async Task SendNotificationAsync(string notifyBaseUrl, int count, string userId)
{
    // 建立 API 請求
    var apiUri = new Uri(notifyBaseUrl + "sendFirst.php?count=" + count + "&userid=" + userId);
    var requestBody = new StringContent("", Encoding.UTF8, "application/json");

    using (var httpClient = new HttpClient())
    {
        // 發送 API 請求
        var httpResponse = await httpClient.PostAsync(apiUri, requestBody);

        if (!httpResponse.IsSuccessStatusCode)
        {
            // 處理錯誤回應
        }
    }

    // 讀取截圖檔案，準備上傳
    await using var stream = System.IO.File.OpenRead("screen.png");

    // 上傳截圖至指定網址
    using (HttpClient httpClient = new HttpClient())
    {
        MultipartFormDataContent formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(stream), "img", "screenshot.png");
        HttpResponseMessage response = await httpClient.PostAsync(notifyBaseUrl + "imgSend.php", formData);
        string result = await response.Content.ReadAsStringAsync();
    }
}

Console.WriteLine($"迴圈運行了 {count} 次。");

await Task.Delay(-1);