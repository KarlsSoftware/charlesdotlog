import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BlogPost } from '../../../models/post.model';
import { PostService } from '../../../services/post.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  posts: BlogPost[] = [];

  constructor(private postService: PostService) {}

  // ngOnInit runs once after the component mounts — good place for initial data fetching
  ngOnInit() {
    this.loadPosts();
  }

  loadPosts() {
    // .subscribe() here (not async pipe) so we can store results in a local array
    // and refresh the list after a delete without leaving and re-entering the route
    this.postService.getPosts().subscribe((posts) => (this.posts = posts));
  }

  onDelete(id: number) {
    if (!confirm('Post wirklich löschen?')) return;
    // After delete, reload the full list so the UI reflects the change immediately
    this.postService.deletePost(id).subscribe(() => this.loadPosts());
  }
}
