<component>
	<property name="quadPosX" type="double" />
	<property name="quadPosY" type="double" />
	<property name="quadSizeX" type="double" />
	<property name="quadSizeY" type="double" />
	<property name="labelPosX" type="double" />
	<property name="labelPosY" type="double" />
	<property name="labelSizeX" type="double" />
	<property name="labelSizeY" type="double" />
	<property name="halign" type="string" />
	<property name="valign" type="string" />
	<property name="textSize" type="double" />

	<template>
		<quad  pos="{{quadPosX}} {{quadPosY}}" size="{{quadSizeX}} {{quadSizeY}}" z-index="1" bgcolor="000CB3FF"/>
		<label pos="{{labelPosX}} {{labelPosY}}" size="{{labelSizeX}} {{labelSizeY}}" z-index="2" text="abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTVWXYZ" textsize="{{textSize}}" textfont="RajdhaniMono" halign="{{halign}}" valign="{{valign}}" />
	</template>
</component>