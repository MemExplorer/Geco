using MPowerKit.VirtualizeListView;

namespace Geco.Views.Helpers;

public class LinearItemsLayoutManager2 : LinearItemsLayoutManager
{
	public event EventHandler? OnFinishedLoadingItems;
	bool _checkItemsLoading;
	public override void InvalidateLayout()
	{
		base.InvalidateLayout();
		if (!_checkItemsLoading)
			Task.Factory.StartNew(RunItemLoadingCheckerTask);
	}

	// Triggers an event after the UI has finished loading all items.
	private Task RunItemLoadingCheckerTask()
	{
		if (base.Adapter == null || base.Adapter.ItemsCount == 0)
			return Task.CompletedTask;

		_checkItemsLoading = true;
		
		// Wait until all items have been loaded into the UI
		while (base.LaidOutItems.Count != base.Adapter?.ItemsCount) ;
		this.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(1), () =>
		{
			OnFinishedLoadingItems?.Invoke(this, EventArgs.Empty);
		});
		_checkItemsLoading = false;
		return Task.CompletedTask;
	}
}
