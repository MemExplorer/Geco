

using Microsoft.Extensions.AI;

namespace Geco.Views.Helpers;
internal class ChatTemplateSelector : DataTemplateSelector
{
	public required DataTemplate ModelChatTemplate { get; set; }
	public required DataTemplate UserChatTemplate { get; set; }
	protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
	{
		if (item is ChatMessage cm && cm.Role == ChatRole.User)
			return UserChatTemplate;

		return ModelChatTemplate;
	}
}
