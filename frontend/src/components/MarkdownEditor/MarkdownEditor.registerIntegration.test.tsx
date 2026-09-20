import { useForm } from 'react-hook-form'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { MarkdownEditor } from './MarkdownEditor'

/**
 * `EditPostForm`/`CommentComposer`/`EditCommentForm` all use bare `{...form.register(...)}`
 * (uncontrolled) rather than `Controller`/`FormField` (controlled). react-hook-form's
 * uncontrolled mode sets the DOM node's initial value imperatively via `ref`, and requires
 * `onChange` to receive an event with `.target.name`/`.target.value` — these tests guard that
 * MarkdownEditor works correctly under that exact contract, not just the controlled one.
 */
function Harness({ defaultBody = '' }: { defaultBody?: string }) {
  const form = useForm({ defaultValues: { bodyMarkdown: defaultBody } })
  return (
    <div>
      <MarkdownEditor {...form.register('bodyMarkdown')} rows={3} />
      <output data-testid="watched">{form.watch('bodyMarkdown')}</output>
    </div>
  )
}

describe('MarkdownEditor + react-hook-form register() (uncontrolled)', () => {
  it('propagates typed text and toolbar-inserted text back into form state', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    const textarea = screen.getByRole('textbox')
    await user.type(textarea, 'hi')
    expect(screen.getByTestId('watched')).toHaveTextContent('hi')

    await user.click(screen.getByRole('button', { name: 'Bold' }))
    expect(screen.getByTestId('watched')).toHaveTextContent('hi**bold text**')
  })

  it('shows the register()-supplied initial value (edit-form scenario) without any typing', async () => {
    const user = userEvent.setup()
    render(<Harness defaultBody="existing comment text" />)

    expect(screen.getByRole('textbox')).toHaveValue('existing comment text')

    await user.click(screen.getByRole('tab', { name: 'Preview' }))
    expect(screen.getByText('existing comment text', { selector: 'p' })).toBeInTheDocument()
  })
})
