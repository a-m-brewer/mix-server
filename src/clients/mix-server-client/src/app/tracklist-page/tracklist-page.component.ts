import {Component, OnInit, ChangeDetectionStrategy} from '@angular/core';
import {TracklistFormComponent} from "./tracklist-form/tracklist-form.component";

@Component({
    selector: 'app-tracklist-page',
    imports: [
        TracklistFormComponent
    ],
    templateUrl: './tracklist-page.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './tracklist-page.component.scss'
})
export class TracklistPageComponent implements OnInit{
    public ngOnInit(): void {
    }
}
