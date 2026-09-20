// Must stay in sync with the backend's Common/Options/PostImageOptions defaults — there's no
// config-fetch endpoint, so these are duplicated deliberately rather than fetched. Shared by every
// image upload surface (post images, community icon/banner), not just posts.
export const MAX_IMAGE_COUNT = 6
export const MAX_IMAGE_SIZE_BYTES = 8 * 1024 * 1024
