import { beforeEach, describe, expect, it } from "vitest";
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MainContentComponent } from './main-content.component';

describe('MainContentComponent', () => {
    let component: MainContentComponent;
    let fixture: ComponentFixture<MainContentComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [MainContentComponent],
            schemas: [NO_ERRORS_SCHEMA]
        })
            .compileComponents();

        fixture = TestBed.createComponent(MainContentComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
