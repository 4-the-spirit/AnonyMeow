import { describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/msw/server'
import { renderWithProviders } from '@/test/test-utils'
import { ImageUploadField } from './ImageUploadField'

const BASE_URL = 'https://test.local'
const BLOB_URL = 'https://blob.test.local/container/uploaded.png'

function mockSasAndBlobUpload() {
  server.use(
    http.post(`${BASE_URL}/api/uploads/images/sas`, () =>
      HttpResponse.json({ uploadUrl: BLOB_URL, blobUrl: BLOB_URL })
    ),
    http.put(BLOB_URL, () => new HttpResponse(null, { status: 201 }))
  )
}

describe('ImageUploadField', () => {
  it('shows a styled dropzone instead of the bare native file input', () => {
    renderWithProviders(<ImageUploadField value={undefined} onChange={vi.fn()} />)

    expect(screen.getByText('Drag and drop an image, or click to browse')).toBeInTheDocument()
    expect(screen.queryByText('No file chosen')).not.toBeInTheDocument()
  })

  it('uploads the selected file and calls onChange with the resulting blob URL', async () => {
    mockSasAndBlobUpload()
    const onChange = vi.fn()
    renderWithProviders(<ImageUploadField value={undefined} onChange={onChange} />)

    const file = new File(['fake-bytes'], 'photo.png', { type: 'image/png' })
    const input = document.querySelector('input[type="file"]') as HTMLInputElement
    await userEvent.upload(input, file)

    await waitFor(() => expect(onChange).toHaveBeenCalledWith(BLOB_URL))
  })

  it('offers a "Replace image" dropzone once a value is set instead of the initial CTA', () => {
    renderWithProviders(<ImageUploadField value={BLOB_URL} onChange={vi.fn()} />)

    expect(screen.getByText('Replace image')).toBeInTheDocument()
    expect(screen.getByAltText('Upload preview')).toHaveAttribute('src', BLOB_URL)
  })
})
