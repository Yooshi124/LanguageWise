using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Security;

public static class ToolScopeFilter
{
	public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> FilterList(
		McpRequestHandler<ListToolsRequestParams, ListToolsResult> next) =>
		async (context, cancellationToken) =>
		{
			var result = await next(context, cancellationToken);
			var scope = GetCaller(context).Scope;
			var ragEnabled = RagEnabled(context);
			result.Tools = scope is null
				? []
				: result.Tools.Where(tool => IsOffered(scope, tool.Name, ragEnabled)).ToList();
			return result;
		};

	public static McpRequestHandler<CallToolRequestParams, CallToolResult> FilterCall(
		McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
		async (context, cancellationToken) =>
		{
			var scope = GetCaller(context).Scope;
			var name = context.Params?.Name;
			if (scope is null || name is null || !IsOffered(scope, name, RagEnabled(context)))
			{
				throw new McpProtocolException($"Unknown tool: '{name}'", McpErrorCode.InvalidParams);
			}
			return await next(context, cancellationToken);
		};

	private static bool IsOffered(string scope, string toolName, bool ragEnabled) =>
		Tools.ToolScopes.Allows(scope, toolName)
		&& (ragEnabled || !toolName.StartsWith(Tools.ToolScopes.SharedPrefix, StringComparison.Ordinal));

	// Docs tools are backed by the RAG server; Rag:Enabled=false (as in CI) hides them.
	private static bool RagEnabled<T>(RequestContext<T> context) =>
		GetServices(context).GetRequiredService<IConfiguration>().GetValue("Rag:Enabled", true);

	private static McpCallerContext GetCaller<T>(RequestContext<T> context) =>
		GetServices(context).GetRequiredService<McpCallerContext>();

	private static IServiceProvider GetServices<T>(RequestContext<T> context) =>
		context.Services ?? throw new InvalidOperationException("No request services.");
}
