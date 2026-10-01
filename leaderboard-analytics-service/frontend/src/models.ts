export interface HostContext {
  readonly user: { id: number; name: string } | null
  signIn(returnUrl?: string): void
  signOut(): Promise<void>
  navigate(path: string): Promise<void>
}
