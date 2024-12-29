export function timespanToTotalSeconds(time?: string | null): number {
  if (!time) {
    return 0;
  }

  // Split into days and time parts
  const [daysPart, timePart] = time.includes('.') ? time.split('.') : [undefined, time];
  const days = daysPart ? Number(daysPart) : 0;

  // Split the time part into hours, minutes, and seconds
  const timeSegments = timePart.split(':').map(Number);

  // Handle missing hours by shifting values
  const [hours = 0, minutes, seconds] = timeSegments.length === 3
    ? timeSegments
    : [0, ...timeSegments]; // Insert 0 for hours if only minutes and seconds are provided

  // Calculate total seconds
  return (days * 24 * 3600) + (hours * 3600) + (minutes * 60) + seconds;
}
