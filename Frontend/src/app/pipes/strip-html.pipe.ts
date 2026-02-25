import { Pipe, PipeTransform, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';

/**
 * Strips HTML tags, decodes entities (e.g. &nbsp;), and truncates at a word boundary.
 * Usage: {{ post.content | stripHtml:160 }}
 */
@Pipe({ name: 'stripHtml', standalone: true })
export class StripHtmlPipe implements PipeTransform {
  private document = inject(DOCUMENT);

  transform(value: string, limit = 0): string {
    if (!value) return '';

    // DOCUMENT token is provided by @angular/platform-server during SSR,
    // so this works in both the browser and the Node.js prerender environment.
    const div = this.document.createElement('div');
    div.innerHTML = value;
    const text = (div.textContent ?? '').replace(/\s+/g, ' ').trim();

    if (!limit || text.length <= limit) return text;

    // Cut at the last space before the limit so no word is split in half
    const cut = text.lastIndexOf(' ', limit);
    return text.slice(0, cut > 0 ? cut : limit) + '…';
  }
}
