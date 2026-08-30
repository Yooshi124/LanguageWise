export interface LanguageOption {
  code: string
  flag: string
  name: string
  color: string
}

export const languageOptions: readonly LanguageOption[] = [
  { code: 'de', flag: 'de.svg', name: 'German', color: '#f59e0b' },
  { code: 'fr', flag: 'fr.svg', name: 'French', color: '#3b82f6' },
  { code: 'it', flag: 'it.svg', name: 'Italian', color: '#16a34a' },
  { code: 'nl', flag: 'nl.svg', name: 'Dutch', color: '#f97316' },
  { code: 'es', flag: 'es.svg', name: 'Spanish', color: '#dc2626' },
  { code: 'pl', flag: 'pl.svg', name: 'Polish', color: '#d4213d' },
]
