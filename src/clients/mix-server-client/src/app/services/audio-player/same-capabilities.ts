export default function sameCapabilities(prev: { [mimeType: string]: boolean }, next: { [mimeType: string]: boolean }) {
  const prevKeys = Object.keys(prev);
  const nextKeys = Object.keys(next);

  if (prevKeys.length !== nextKeys.length) return false;

  return prevKeys.every(key => key in next && prev[key] === next[key]);
}
