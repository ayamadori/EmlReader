﻿//
// MultipartRelatedWebViewClient.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2014-2020 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using MimeKit;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace EmlReader
{
	/// <summary>
	/// A WebViewClient based on a multipart/related stack.
	/// </summary>
	public class MultipartRelatedWebViewClient
	{
		readonly IList<MultipartRelated> stack;

		public MultipartRelatedWebViewClient (IList<MultipartRelated> stack)
		{
			// Note: we need to clone the multipart/related stack because Android
			// may not render the HTML immediately and we need to preserve state
			// until it does.
			this.stack = new List<MultipartRelated> (stack);
		}

        //public void ShouldInterceptRequest(WebView view, WebViewWebResourceRequestedEventArgs args)
        public async void ShouldInterceptRequest(CoreWebView2 view, CoreWebView2WebResourceRequestedEventArgs args)
        {
            //var uri = args.Request.RequestUri;
            var uri = new Uri(args.Request.Uri);
            int index;

			// walk up our multipart/related stack looking for the MIME part for the requested URL
			for (int i = stack.Count - 1; i >= 0; i--) {
				var related = stack[i];

				if ((index = related.IndexOf (uri)) == -1)
					continue;

				if (related[index] is MimePart part) {
					var mimeType = part.ContentType.MimeType;
					var charset = part.ContentType.Charset;
					var stream = part.Content.Open();

					// construct our response containing the decoded content
					//HttpResponseMessage _response = new HttpResponseMessage();
					//_response.Content = new HttpStreamContent(stream.AsInputStream());
					//args.Response = _response;
					CoreWebView2Environment cwv2e = await CoreWebView2Environment.CreateAsync();
					// https://stackoverflow.com/questions/7669311/is-there-a-way-to-convert-a-system-io-stream-to-a-windows-storage-streams-irandovar memStream = new MemoryStream();
					var memStream = new MemoryStream();
					await stream.CopyToAsync(memStream);
					memStream.Position = 0;
					args.Response = cwv2e.CreateWebResourceResponse(memStream.AsRandomAccessStream(), 200, "OK", $"Content-Type: {mimeType}; charset={charset}");                   
				}
			}
		}
	}
}
