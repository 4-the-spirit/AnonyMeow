import { useForm } from 'react-hook-form'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { MarkdownEditor } from './MarkdownEditor'

/**
 * `FormControl` is a Radix `Slot` that merges `id`/`aria-describedby`/`aria-invalid` onto
 * whatever it wraps. Because MarkdownEditor's root element is the Tabs wrapper rather than
 * the textarea itself, those props must be explicitly forwarded down to the real `<textarea>`
 * — otherwise `<FormLabel htmlFor>` (and e2e helpers using `getByLabel`) can't find it.
 */
function Harness() {
  const form = useForm({ defaultValues: { bodyMarkdown: '' } })
  return (
    <Form {...form}>
      <FormField
        control={form.control}
        name="bodyMarkdown"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Body</FormLabel>
            <FormControl>
              <MarkdownEditor {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
    </Form>
  )
}

describe('MarkdownEditor inside FormField/FormControl/FormLabel', () => {
  it('associates the label with the actual textarea, and typing fills it', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const textarea = screen.getByLabelText('Body')
    expect(textarea.tagName).toBe('TEXTAREA')

    await user.type(textarea, 'hello world')
    expect(textarea).toHaveValue('hello world')
  })
})
