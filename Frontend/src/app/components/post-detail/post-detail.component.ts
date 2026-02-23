import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PostService } from '../../services/post.service';
import { BlogPost } from '../../models/post.model';
import { Observable, map, switchMap } from 'rxjs';
@Component({
  selector: 'app-post-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './post-detail.component.html',
  styleUrl: './post-detail.component.css',
})
export class PostDetailComponent {
  /**
   * ActivatedRoute = Angular's equivalent of [FromRoute] in .NET.
   * switchMap: when the route param changes, cancel the old HTTP request
   * and start a new one — prevents stale data.
   */
  post$: Observable<BlogPost>;

  constructor(
    private route: ActivatedRoute,
    private postService: PostService,
  ) {
    this.post$ = this.route.params.pipe(
      switchMap((params) => this.postService.getPost(+params['id'])),
      map(post => ({ ...post, content: post.content.replace(/&nbsp;/g, ' ') })),
    );
  }
}
