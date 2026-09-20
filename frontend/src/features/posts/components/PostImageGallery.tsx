interface PostImageGalleryProps {
  imageUrls: string[]
  alt: string
}

export function PostImageGallery({ imageUrls, alt }: PostImageGalleryProps) {
  const [first, ...rest] = imageUrls

  return (
    <div className="flex flex-col gap-2">
      <img src={first} alt={alt} className="max-h-128 rounded-lg" />
      {rest.length > 0 && (
        <div className="grid grid-cols-3 gap-2">
          {rest.map((url) => (
            <img key={url} src={url} alt={alt} className="h-24 w-full rounded-lg object-cover" />
          ))}
        </div>
      )}
    </div>
  )
}
