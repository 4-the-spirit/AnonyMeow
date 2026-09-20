import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { SearchBar } from './SearchBar'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname + location.search}</div>
}

describe('SearchBar', () => {
  it('navigates to /search with the trimmed, encoded query on submit', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <SearchBar />
        <LocationDisplay />
      </MemoryRouter>
    )

    await userEvent.type(screen.getByRole('textbox'), '  hello world  {Enter}')

    expect(screen.getByTestId('location')).toHaveTextContent('/search?q=hello%20world')
  })

  it('does not navigate for a blank query', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <SearchBar />
        <LocationDisplay />
      </MemoryRouter>
    )

    await userEvent.type(screen.getByRole('textbox'), '   {Enter}')

    expect(screen.getByTestId('location')).toHaveTextContent('/')
  })
})
