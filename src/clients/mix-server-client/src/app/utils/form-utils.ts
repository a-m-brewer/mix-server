import {AbstractControl, FormArray, FormGroup} from "@angular/forms";

export function markAllAsDirty(control: AbstractControl) {
  control.markAsDirty(); // Mark the current control as dirty
  if (control instanceof FormGroup) {
    Object.keys(control.controls).forEach((key) => {
      markAllAsDirty(control.controls[key]); // Recursively mark child controls
    });
  } else if (control instanceof FormArray) {
    control.controls.forEach((childControl) => {
      markAllAsDirty(childControl); // Recursively mark child controls
    });
  }
}
