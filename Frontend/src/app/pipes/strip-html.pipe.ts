import { Pipe, PipeTransform } from '@angular/core';

/**
 * Strips HTML tags, decodes entities (e.g. &nbsp;), and truncates at a word boundary.
 * Usage: {{ post.content | stripHtml:160 }}
 */
@Pipe({ name: 'stripHtml', standalone: true })
export class StripHtmlPipe implements PipeTransform {
  transform(value: string, limit = 0): string {
    if (!value) return '';

    // Use the browser DOM to strip tags and decode HTML entities cleanly
    const div = document.createElement('div');
    div.innerHTML = value;
    const text = (div.textContent ?? '').replace(/\s+/g, ' ').trim();

    if (!limit || text.length <= limit) return text;

    // Cut at the last space before the limit so no word is split in half
    const cut = text.lastIndexOf(' ', limit);
    return text.slice(0, cut > 0 ? cut : limit) + '…';
  }
}
