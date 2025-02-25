using Geco.Views.Helpers;
using Microsoft.Maui.Handlers;
using WebView = Android.Webkit.WebView;

namespace Geco.Platforms.Android.Handlers;

public class ContentSizedWebViewHandler : WebViewHandler
{
	private Size _contentSize;

	public static new IPropertyMapper<IWebView, IWebViewHandler> Mapper =
		new PropertyMapper<IWebView, IWebViewHandler>(WebViewHandler.Mapper)
		{
			["WebViewClient"] = MapContentSizedWebViewClient, ["WebChromeClient"] = MapContentSizedWebChromeClient
		};

	public static void MapContentSizedWebChromeClient(IWebViewHandler handler, IWebView webView)
	{
		if (handler is ContentSizedWebViewHandler platformHandler)
			handler.PlatformView.SetWebChromeClient(new ContentSizedWebChromeClient(platformHandler));
	}

	public static void MapContentSizedWebViewClient(IWebViewHandler handler, IWebView webView)
	{
		if (handler is ContentSizedWebViewHandler platformHandler)
			handler.PlatformView.SetWebViewClient(new ContentSizedWebViewClient(platformHandler));
	}

	public new ContentSizedWebView VirtualView => (ContentSizedWebView)base.VirtualView;

	public ContentSizedWebViewHandler() : base(Mapper)
	{
	}

	protected override WebView CreatePlatformView()
	{
		var platformView = base.CreatePlatformView();
		platformView.AddJavascriptInterface(new ContentSizeObserverBridge(this), nameof(ContentSizedWebViewHandler));

		return platformView;
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		double scrollViewContentWidth = _contentSize.Width;
		double scrollViewContentHeight = _contentSize.Height;
		double width;
		double height;

		if (widthConstraint <= 0 || double.IsInfinity(widthConstraint))
			width = scrollViewContentWidth;
		else
			width = Math.Min(widthConstraint, scrollViewContentWidth);

		if (heightConstraint <= 0 || double.IsInfinity(heightConstraint))
			height = scrollViewContentHeight;
		else
			height = Math.Min(heightConstraint, scrollViewContentHeight);

		return new Size(width, height);
	}

	public void OnContentSizeChanged(Size size)
	{
		_contentSize = size;
		Invoke(nameof(IView.InvalidateMeasure), null);
	}
}
