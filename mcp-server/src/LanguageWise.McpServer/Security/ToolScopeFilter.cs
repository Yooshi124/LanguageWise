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
			result.Tools = scope is null
				? []
				: result.Tools.Where(tool => Tools.ToolScopes.Allows(scope, tool.Name)).ToList();
			return result;
		};

	public static McpRequestHandler<CallToolRequestParams, CallToolResult> FilterCall(
		McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
		async (context, cancellationToken) =>
		{
			var scope = GetCaller(context).Scope;
			var name = context.Params?.Name;
			if (scope is null || name is null || !Tools.ToolScopes.Allows(scope, name))
			{
				throw new McpProtocolException($"Unknown tool: '{name}'", McpErrorCode.InvalidParams);
			}
			return await next(context, cancellationToken);
		};

	private static McpCallerContext GetCaller<T>(RequestContext<T> context) =>
		(context.Services ?? throw new InvalidOperationException("No request services."))
			.GetRequiredService<McpCallerContext>();
}
