import { cleanupDue } from './control.mjs';

export async function dispatch({ readControl, startCleanup, now = Date.now }) {
  const state = await readControl();
  if (!cleanupDue(state, now())) return { started: false };
  await startCleanup();
  return { started: true };
}

export async function handler() {
  const [{ DynamoDBClient, GetItemCommand }, { CodeBuildClient, StartBuildCommand }] = await Promise.all([
    import('@aws-sdk/client-dynamodb'), import('@aws-sdk/client-codebuild'),
  ]);
  const table = process.env.CONTROL_TABLE_NAME;
  const project = process.env.CLEANUP_PROJECT_NAME;
  if (!table || !project) throw new Error('Dispatcher configuration missing');
  const dynamodb = new DynamoDBClient({});
  const codebuild = new CodeBuildClient({});
  return dispatch({
    async readControl() {
      const result = await dynamodb.send(new GetItemCommand({ TableName: table, Key: { id: { S: 'control' } }, ConsistentRead: true }));
      return result.Item?.payload ? JSON.parse(result.Item.payload.S) : undefined;
    },
    startCleanup: () => codebuild.send(new StartBuildCommand({ projectName: project })),
  });
}