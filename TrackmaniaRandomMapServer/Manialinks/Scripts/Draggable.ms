﻿<!--
#Include "TextLib" as TextLib

Void __DoNothing() {}

main() {
    declare Boolean MouseIsDown = False;
    declare Vec2 MouseLastPos = <MouseX, MouseY>;
    declare Vec2 MouseDragStart = <0., 0.>;

    // set to true before initialization if scripts wants to terminate immediately
    declare Boolean ScriptRunning for This = False;
    ScriptRunning = True;

    declare WindowId = "{{ id }}";
    declare WindowInstance <=> Page.MainFrame.GetFirstChild(WindowId) as CMlFrame;

    {
        declare Boolean IsFocused for WindowInstance = False;
        declare Vec2 DragStartPosition for WindowInstance = WindowInstance.RelativePosition_V3;
    }
    
    while (ScriptRunning) {
        yield;

        if (!PageIsVisible) {
            continue;
        }
        
        foreach (Event in PendingEvents) {
            switch (Event.Type) {
                case CMlScriptEvent::Type::KeyPress: { +++ OnKeyPress +++ }
                case CMlScriptEvent::Type::MouseClick: { +++ OnMouseClick +++ }
                case CMlScriptEvent::Type::MouseRightClick: {  +++ OnMouseRightClick +++  }
                case CMlScriptEvent::Type::MouseOver: {  +++ OnMouseOver +++  }
                case CMlScriptEvent::Type::MouseOut: {  +++ OnMouseOut +++  }
                case CMlScriptEvent::Type::EntrySubmit: {  +++ OnEntrySubmit +++  }
                case CMlScriptEvent::Type::MenuNavigation: {  +++ OnMenuNavigation +++  }
                case CMlScriptEvent::Type::PluginCustomEvent: {  +++ OnPluginCustomEvent +++  }
            }
            
            +++ OnEvent +++
        }
        
        foreach (Event in Input.PendingEvents) {
            +++ OnInputEvent +++
        }
        
        foreach (Event in Http.PendingEvents) {
            +++ OnHttpEvent +++
        }
        
        if (MouseLeftButton && !MouseIsDown) {
            MouseIsDown = True;
            MouseDragStart = <MouseX, MouseY>;
            
            declare IsFocused for WindowInstance = False;
            declare DragStartPosition for WindowInstance = WindowInstance.RelativePosition_V3;
    
            DragStartPosition = WindowInstance.RelativePosition_V3;
    
            if (MouseX >= DragStartPosition.X && MouseX <= DragStartPosition.X + WindowInstance.Size.X
                && MouseY <= DragStartPosition.Y && MouseY >= DragStartPosition.Y - 5) {
                IsFocused = True;
            }
        } else if (!MouseLeftButton && MouseIsDown) {
            MouseIsDown = False;            
            declare IsFocused for WindowInstance = False;
            IsFocused = False;
        }
        
        if (MouseX != MouseLastPos.X || MouseY != MouseLastPos.Y) {
            { +++ OnMouseMove +++ }
            
            if (MouseIsDown) {
                declare IsFocused for WindowInstance = False;
    
                if (IsFocused) {
                    declare DragStartPosition for WindowInstance = WindowInstance.RelativePosition_V3;
        
                    declare NewX = DragStartPosition.X + (MouseX - MouseDragStart.X);
                    declare NewY = DragStartPosition.Y + (MouseY - MouseDragStart.Y);
        
                    WindowInstance.RelativePosition_V3 = <NewX, NewY>;
                }
            }
            
            MouseLastPos = <MouseX, MouseY>;
        }
        
        +++ OnLoop +++
    }
    
    +++ OnScriptExecutionFinished +++
    
    log("Script execution finished.");
}
-->