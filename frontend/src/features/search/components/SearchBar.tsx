import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Search } from 'lucide-react'
import { Input } from '@/components/ui/input'

export function SearchBar() {
  const { t } = useTranslation('search')
  const navigate = useNavigate()
  const [value, setValue] = useState('')

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (!trimmed) return
    navigate(`/search?q=${encodeURIComponent(trimmed)}`)
  }

  return (
    <form onSubmit={handleSubmit} className="relative">
      <Search className="text-muted-foreground pointer-events-none absolute start-2 top-1/2 size-4 -translate-y-1/2" />
      <Input
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={t('searchBar.placeholder')}
        aria-label={t('searchBar.placeholder')}
        className="w-28 ps-8 sm:w-44 md:w-56"
      />
    </form>
  )
}
