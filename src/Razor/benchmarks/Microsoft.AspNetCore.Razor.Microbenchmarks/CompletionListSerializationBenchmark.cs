﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks
{
    public class CompletionListSerializationBenchmark : TagHelperBenchmarkBase
    {
        private readonly byte[] _completionListBuffer;

        public CompletionListSerializationBenchmark()
        {
            var tagHelperFactsService = new DefaultTagHelperFactsService();
            var completionService = new DefaultTagHelperCompletionService(tagHelperFactsService);
            var htmlFactsService = new DefaultHtmlFactsService();
            var tagHelperCompletionProvider = new TagHelperCompletionProvider(completionService, htmlFactsService, tagHelperFactsService);

            var documentContent = "<";
            var queryIndex = 1;
            CompletionList = GenerateCompletionList(documentContent, queryIndex, tagHelperCompletionProvider);
            _completionListBuffer = GenerateBuffer(CompletionList);

            Serializer = new LspSerializer();
            Serializer.RegisterRazorConverters();
        }

        private LspSerializer Serializer { get; }

        private CompletionList CompletionList { get; }

        [Benchmark(Description = "Component Completion List Roundtrip Serialization")]
        public void ComponentElement_CompletionList_Serialization_RoundTrip()
        {
            // Serialize back to json.
            MemoryStream originalStream;
            using (originalStream = new MemoryStream())
            using (var writer = new StreamWriter(originalStream, Encoding.UTF8, bufferSize: 4096))
            {
                Serializer.JsonSerializer.Serialize(writer, CompletionList);
            }

            CompletionList deserializedCompletions;
            var stream = new MemoryStream(originalStream.GetBuffer());
            using (stream)
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                deserializedCompletions = Serializer.JsonSerializer.Deserialize<CompletionList>(reader);
            }
        }

        [Benchmark(Description = "Component Completion List Serialization")]
        public void ComponentElement_CompletionList_Serialization()
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096);
            Serializer.JsonSerializer.Serialize(writer, CompletionList);
        }

        [Benchmark(Description = "Component Completion List Deserialization")]
        public void ComponentElement_CompletionList_Deserialization()
        {
            // Deserialize from json file.
            using var stream = new MemoryStream(_completionListBuffer);
            using var reader = new JsonTextReader(new StreamReader(stream));
            CompletionList deserializedCompletions;
            deserializedCompletions = Serializer.JsonSerializer.Deserialize<CompletionList>(reader);
        }

        private CompletionList GenerateCompletionList(string documentContent, int queryIndex, TagHelperCompletionProvider componentCompletionProvider)
        {
            var sourceDocument = RazorSourceDocument.Create(documentContent, RazorSourceDocumentProperties.Default);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);
            var tagHelperDocumentContext = TagHelperDocumentContext.Create(prefix: string.Empty, DefaultTagHelpers);

            var completionQueryLocation = new SourceSpan(queryIndex, length: 0);
            var context = new RazorCompletionContext(syntaxTree, tagHelperDocumentContext);

            var razorCompletionItems = componentCompletionProvider.GetCompletionItems(context, completionQueryLocation);
            var completionList = RazorCompletionEndpoint.CreateLSPCompletionList(
                razorCompletionItems,
                new CompletionListCache(),
                new[] { ExtendedCompletionItemKinds.TagHelper },
                new PlatformAgnosticCompletionCapability()
                {
                    VSCompletionList = new VSCompletionListCapability()
                    {
                        CommitCharacters = true,
                        Data = true,
                    }
                });
            return completionList;
        }

        private byte[] GenerateBuffer(CompletionList completionList)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096);
            Serializer.JsonSerializer.Serialize(writer, completionList);
            var buffer = stream.GetBuffer();

            return buffer;
        }
    }
}
