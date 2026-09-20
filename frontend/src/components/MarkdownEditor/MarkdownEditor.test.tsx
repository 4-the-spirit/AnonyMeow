import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MarkdownEditor } from './MarkdownEditor'

function ControlledEditor({ initialValue = '' }: { initialValue?: string }) {
  const [value, setValue] = useState(initialValue)
  return <MarkdownEditor value={value} onChange={(event) => setValue(event.target.value)} />
}

describe('MarkdownEditor', () => {
  it('inserts placeholder bold text at the cursor when nothing is selected', async () => {
    const user = userEvent.setup()
    render(<ControlledEditor />)

    const textarea = screen.getByRole('textbox')
    await user.type(textarea, 'hello')
    await user.click(screen.getByRole('button', { name: 'Bold' }))

    expect(textarea).toHaveValue('hello**bold text**')
  })

  it('wraps the current selection in bold markers when text is selected', async () => {
    const user = userEvent.setup()
    render(<ControlledEditor initialValue="hello" />)

    const textarea = screen.getByRole('textbox') as HTMLTextAreaElement
    textarea.focus()
    textarea.setSelectionRange(0, 5)
    await user.click(screen.getByRole('button', { name: 'Bold' }))

    expect(textarea).toHaveValue('**hello**')
  })

  it('renders the current value through MarkdownBody on the Preview tab', async () => {
    const user = userEvent.setup()
    render(<ControlledEditor initialValue="**strong text**" />)

    await user.click(screen.getByRole('tab', { name: 'Preview' }))

    expect(screen.getByText('strong text').tagName).toBe('STRONG')
  })

  it('shows a placeholder message when previewing empty content', async () => {
    const user = userEvent.setup()
    render(<ControlledEditor initialValue="" />)

    await user.click(screen.getByRole('tab', { name: 'Preview' }))

    expect(screen.getByText('Nothing to preview')).toBeInTheDocument()
  })

  it('inserts an emoji at the cursor via the emoji picker', async () => {
    const user = userEvent.setup()
    render(<ControlledEditor initialValue="hi " />)

    const textarea = screen.getByRole('textbox') as HTMLTextAreaElement
    textarea.focus()
    textarea.setSelectionRange(3, 3)
    await user.click(screen.getByRole('button', { name: 'Emoji' }))
    await user.click(screen.getAllByText('🔥')[0])

    expect(textarea).toHaveValue('hi 🔥')
  })
})
