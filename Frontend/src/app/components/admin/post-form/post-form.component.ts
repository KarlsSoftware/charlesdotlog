import { Component, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PostService } from '../../../services/post.service';
import { QuillModule } from 'ngx-quill';

@Component({
  selector: 'app-post-form',
  standalone: true,
  imports: [FormsModule, QuillModule],
  templateUrl: './post-form.component.html',
})
export class PostFormComponent implements OnInit {
  // Form field values — each is two-way bound to an input via [(ngModel)]
  title = '';
  content = '';
  author = '';
  isPublished = true;

  isEdit = false;        // true when the URL contains an :id param (edit mode)
  private editId?: number;

  quillModules = {
    toolbar: [
      ['bold', 'italic'],
      [{ header: [2, 3, false] }],
      [{ list: 'ordered' }, { list: 'bullet' }],
      ['link'],
      ['clean']
    ]
  };

  constructor(
    private postService: PostService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    // If the URL has an :id param (e.g. /admin/edit/3) we're in edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.editId = +id; // + converts the route string param to a number
      // Pre-fill the form fields with the existing post's data
      this.postService.getPost(this.editId).subscribe((post) => {
        this.title = post.title;
        this.content = post.content;
        this.author = post.author;
        this.isPublished = post.isPublished;
      });
    }
  }

  onSubmit(form: NgForm) {
    if (form.invalid) {
      form.control.markAllAsTouched();
      return;
    }

    const data = {
      title: this.title,
      content: this.content,
      author: this.author,
      isPublished: this.isPublished,
    };

    // Same .subscribe() handler works for both create and update
    const request = this.isEdit
      ? this.postService.updatePost(this.editId!, data) // ! = editId is guaranteed set when isEdit is true
      : this.postService.createPost(data);

    request.subscribe({
      next: () => this.router.navigate(['/admin']),
      error: (err) => console.error('Fehler beim Speichern:', err.status, err.error),
    });
  }

  /** Navigate back to the dashboard without saving */
  onCancel() {
    this.router.navigate(['/admin']);
  }
}
