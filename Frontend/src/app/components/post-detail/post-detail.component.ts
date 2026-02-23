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
   * Gives access to the URL parameters of the currently active route.
   *
   * switchMap: every time the route params change (e.g. user navigates from
   * post/1 to post/2), it cancels the in-flight HTTP request for the old id
   * and immediately starts a new one — prevents stale data from arriving late.
   */
  post$: Observable<BlogPost>;

  constructor(
    private route: ActivatedRoute,
    private postService: PostService,
  ) {
    this.post$ = this.route.params.pipe(
      switchMap((params) =>
        // The unary + operator converts the route param string (e.g. "42") to a number.
        // Route params are always strings — the API expects a number.
        this.postService.getPost(+params['id'])
      ),
      // { ...post } is the spread operator — it creates a new object that copies all
      // fields from `post`, then overrides `content`. We never mutate the original
      // object because Observables are expected to emit immutable values.
      map(post => ({ ...post, content: post.content.replace(/&nbsp;/g, ' ') })),
    );
  }
}
