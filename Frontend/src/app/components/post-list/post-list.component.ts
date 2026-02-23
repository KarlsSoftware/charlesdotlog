import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PostService } from '../../services/post.service';
import { BlogPost } from '../../models/post.model';
import { Observable } from 'rxjs';
import { StripHtmlPipe } from '../../pipes/strip-html.pipe';

@Component({
  selector: 'app-post-list',
  standalone: true,
  imports: [CommonModule, RouterLink, StripHtmlPipe],
  templateUrl: './post-list.component.html',
  styleUrl: './post-list.component.css',
})
export class PostListComponent {
  /**
   * posts$ is an Observable — the $ suffix is an Angular convention
   * to mark reactive streams. The async pipe in the template
   * subscribes automatically and unsubscribes on destroy (no memory leak).
   */
  posts$: Observable<BlogPost[]>;

  constructor(private postService: PostService) {
    this.posts$ = this.postService.getPosts();
  }
}
