/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.Mvc.Testing;
using Rhetos.Host.AspNet.RestApi.Test.Tools;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TestAction;
using TestApp;
using Xunit;
using Xunit.Abstractions;

namespace Rhetos.Host.AspNet.RestApi.Test
{
    public class ErrorReportingTest : IDisposable
    {
        // TODO: It seems that new RhetosHost is created for each test method run, which hinders test performance.
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly ITestOutputHelper output;

        public ErrorReportingTest(ITestOutputHelper output)
        {
            _factory = new CustomWebApplicationFactory<Startup>();
            this.output = output;
        }

        public void Dispose()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
        }

        [Theory]
        [InlineData("test1", "test2",
            @"400 {""UserMessage"":""test1"",""SystemMessage"":""test2""}",
            "[Trace]|Rhetos.UserException: test1|MessageParameters: null|SystemMessage: test2")]
        [InlineData("test1", null,
            @"400 {""UserMessage"":""test1"",""SystemMessage"":null}",
            "[Trace]|Rhetos.UserException: test1|MessageParameters: null|SystemMessage: ")]
        [InlineData(null, null,
            @"400 {""UserMessage"":""Exception of type 'Rhetos.UserException' was thrown."",""SystemMessage"":null}",
            "[Trace]|Rhetos.UserException: Exception of type 'Rhetos.UserException' was thrown.|MessageParameters: null|SystemMessage: ")]
        public async Task UserExceptionResponse(string testUserMessage, string testSystemMessage, string expectedResponse, string expectedLogPatterns)
        {
            var logEntries = new LogEntries();
            var client = _factory
                .WithWebHostBuilder(builder => builder.MonitorLogging(logEntries))
                .CreateClient();
            var requestData = new ReturnUserError { TestUserMessage = testUserMessage, TestSystemMessage = testSystemMessage };
            var response = await client.PostAsync("rest/TestAction/ReturnUserError", JsonContent.Create(requestData));
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal<object>(expectedResponse, $"{(int)response.StatusCode} {responseContent}");

            output.WriteLine(string.Join(Environment.NewLine, logEntries));
            string[] exceptedLogPatterns = expectedLogPatterns.Split('|');
            Assert.Equal(1, logEntries.Select(e => e.ToString()).Count(
                // The command summary is not reported by ProcessingEngine for UserExceptions to improved performance.
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task ClientExceptionResponse()
        {
            var logEntries = new LogEntries();
            var client = _factory
                .WithWebHostBuilder(builder => builder.MonitorLogging(logEntries))
                .CreateClient();
            var response = await client.PostAsync("rest/TestAction/ReturnClientError", JsonContent.Create(new { }));
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal<object>(
                "400 {\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\""
                    + ",\"SystemMessage\":\"test exception\"}",
                $"{(int)response.StatusCode} {responseContent}");

            output.WriteLine(string.Join(Environment.NewLine, logEntries));
            string[] exceptedLogPatterns = new[] {
                "[Information]",
                "Rhetos.ClientException: test exception",
                "Command: ExecuteActionCommandInfo TestAction.ReturnClientError" };
            Assert.Equal(1, logEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task ServerExceptionResponse()
        {
            var logEntries = new LogEntries();
            var client = _factory
                .WithWebHostBuilder(builder => builder.MonitorLogging(logEntries))
                .CreateClient();

            Guid[] ids = new Guid[] {
                new Guid("15a1b223-aa2b-448a-9ddd-b4384188c489"),
                new Guid("25a1b223-aa2b-448a-9ddd-b4384188c489") };
            var response = await client.GetAsync(
                $"rest/TestAction/ReturnServerError/?filters=[{{\"Filter\":\"Guid[]\",\"Value\":{JsonSerializer.Serialize(ids)}}}]");
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.StartsWith
                ("500 {\"UserMessage\":null,\"SystemMessage\":\"Internal server error occurred. See server log for more information. (ArgumentException, " + DateTime.Now.ToString("yyyy-MM-dd"),
                $"{(int)response.StatusCode} {responseContent}");

            output.WriteLine(string.Join(Environment.NewLine, logEntries));
            string[] exceptedLogPatterns = new[] {
                "[Error]",
                "System.ArgumentException: test exception",
                "Command: ReadCommandInfo TestAction.ReturnServerError records, filters: System.Guid[] \"2 items: 15a1b223-aa2b-448a-9ddd-b4384188c489 ...\"" };
            Assert.Equal(1, logEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }

        [Fact]
        public async Task InvalidWebRequestFormatResponse()
        {
            var logEntries = new LogEntries();
            var client = _factory
                .WithWebHostBuilder(builder => builder.MonitorLogging(logEntries))
                .CreateClient();

            var response = await client.GetAsync("rest/TestAction/ReturnServerError/?filters=[{0}");
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal<object>(
                "400 {\"UserMessage\":\"Operation could not be completed because the request sent to the server was not valid or not properly formatted.\""
                    + ",\"SystemMessage\":\"The provided filter parameter has invalid JSON format: Invalid JavaScript property identifier character: }. Path '[0]', line 1, position 3. Filter parameter: [{0}\"}",
                $"{(int)response.StatusCode} {responseContent}");

            output.WriteLine(string.Join(Environment.NewLine, logEntries));
            string[] exceptedLogPatterns = new[] {
                "[Information]",
                "Rhetos.ClientException: The provided filter",
                "Filter parameter: [{0}"
                // The command summary is not reported by ProcessingEngine, because the ClientException occurred before the command was constructed.
            };
            Assert.Equal(1, logEntries.Select(e => e.ToString()).Count(
                entry => exceptedLogPatterns.All(pattern => entry.Contains(pattern))));
        }
    }
}
