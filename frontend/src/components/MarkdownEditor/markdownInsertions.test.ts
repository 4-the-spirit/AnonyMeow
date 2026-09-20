import { describe, expect, it } from 'vitest'
import { applyLinkWithUrl, applyMarkdownAction } from './markdownInsertions'

describe('applyMarkdownAction', () => {
  it('wraps a selection in bold markers', () => {
    const result = applyMarkdownAction('hello world', 6, 11, 'bold')
    expect(result.text).toBe('hello **world**')
    expect(result.text.slice(result.selectionStart, result.selectionEnd)).toBe('world')
  })

  it('inserts placeholder bold text when nothing is selected', () => {
    const result = applyMarkdownAction('', 0, 0, 'bold')
    expect(result.text).toBe('**bold text**')
    expect(result.text.slice(result.selectionStart, result.selectionEnd)).toBe('bold text')
  })

  it('toggles bold off when the selection is already wrapped', () => {
    const result = applyMarkdownAction('**world**', 2, 7, 'bold')
    expect(result.text).toBe('world')
  })

  it('wraps a selection in italic markers', () => {
    const result = applyMarkdownAction('hi there', 3, 8, 'italic')
    expect(result.text).toBe('hi _there_')
  })

  it('wraps a selection in inline code markers', () => {
    const result = applyMarkdownAction('run npm install', 4, 15, 'code')
    expect(result.text).toBe('run `npm install`')
  })

  it('turns a selection into a markdown link with the url pre-selected', () => {
    const result = applyMarkdownAction('see docs', 4, 8, 'link')
    expect(result.text).toBe('see [docs](https://)')
    expect(result.text.slice(result.selectionStart, result.selectionEnd)).toBe('https://')
  })

  it('inserts placeholder link text when nothing is selected', () => {
    const result = applyMarkdownAction('', 0, 0, 'link')
    expect(result.text).toBe('[link text](https://)')
  })

  it('prefixes a single line with a bullet', () => {
    const result = applyMarkdownAction('milk', 0, 4, 'bulletList')
    expect(result.text).toBe('- milk')
  })

  it('numbers each non-empty line in a multi-line selection', () => {
    const text = 'milk\neggs\n\nbread'
    const result = applyMarkdownAction(text, 0, text.length, 'numberedList')
    expect(result.text).toBe('1. milk\n2. eggs\n\n3. bread')
  })

  it('quotes every non-empty line in a multi-line selection', () => {
    const text = 'line one\nline two'
    const result = applyMarkdownAction(text, 0, text.length, 'blockquote')
    expect(result.text).toBe('> line one\n> line two')
  })

  it('only affects lines touched by a partial mid-text selection', () => {
    const text = 'first\nsecond\nthird'
    // selection sits inside "second"
    const result = applyMarkdownAction(text, 6, 12, 'bulletList')
    expect(result.text).toBe('first\n- second\nthird')
  })
})

describe('applyLinkWithUrl', () => {
  it('builds a markdown link from separate text and url fields', () => {
    const result = applyLinkWithUrl('see ', 4, 4, 'the docs', 'https://example.com')
    expect(result.text).toBe('see [the docs](https://example.com)')
  })

  it('falls back to the url as the link text when no text was given', () => {
    const result = applyLinkWithUrl('', 0, 0, '', 'https://example.com')
    expect(result.text).toBe('[https://example.com](https://example.com)')
  })

  it('replaces the original selection rather than appending after it', () => {
    const result = applyLinkWithUrl('check this out', 6, 10, 'link', 'https://example.com')
    expect(result.text).toBe('check [link](https://example.com) out')
  })

  it('places the cursor right after the inserted link', () => {
    const result = applyLinkWithUrl('', 0, 0, 'docs', 'https://example.com')
    expect(result.selectionStart).toBe(result.text.length)
    expect(result.selectionEnd).toBe(result.text.length)
  })
})
