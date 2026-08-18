import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

export function youtubeId(url?: string | null): string | null {
  if (!url) {
    return null;
  }
  const match = url.match(
    /(?:youtu\.be\/|youtube(?:-nocookie)?\.com\/(?:watch\?v=|embed\/|shorts\/))([\w-]{11})/,
  );
  return match?.[1] ?? null;
}

export function vimeoId(url?: string | null): string | null {
  if (!url) {
    return null;
  }
  const match = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
  return match?.[1] ?? null;
}

export function isDirectVideo(url: string): boolean {
  return /\.(mp4|webm|ogg)(\?|#|$)/i.test(url);
}

@Component({
  selector: 'app-video-embed',
  template: `
    @if (safeFrame(); as src) {
      <iframe
        [src]="src"
        title="Lecture video"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
        loading="lazy"
      ></iframe>
    } @else if (fileUrl()) {
      <video controls playsinline [src]="fileUrl()!"></video>
    } @else {
      <ng-content />
    }
  `,
  styles: `
    :host { display: block; width: 100%; height: 100%; }
    iframe, video { width: 100%; height: 100%; border: 0; display: block; }
  `,
})
export class VideoEmbed {
  private readonly sanitizer = inject(DomSanitizer);
  readonly url = input<string | null | undefined>(null);

  readonly fileUrl = computed(() => {
    const value = this.url();
    return value && isDirectVideo(value) ? value : null;
  });

  readonly safeFrame = computed((): SafeResourceUrl | null => {
    const value = this.url();
    const yt = youtubeId(value);
    if (yt) {
      return this.sanitizer.bypassSecurityTrustResourceUrl(
        `https://www.youtube-nocookie.com/embed/${yt}?rel=0&modestbranding=1`,
      );
    }
    const vimeo = vimeoId(value);
    if (vimeo) {
      return this.sanitizer.bypassSecurityTrustResourceUrl(`https://player.vimeo.com/video/${vimeo}`);
    }
    return null;
  });
}
